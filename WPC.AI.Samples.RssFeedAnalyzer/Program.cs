//
// Copyright (c) Gianni Rosa Gallina. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace WPC.AI.Samples.RssFeedAnalyzer
{
    using System;
    using System.Threading.Tasks;
    using Services;
    using Microsoft.Extensions.Configuration;
    using System.IO;
    using Newtonsoft.Json;
    using System.Reflection;
    using WPC.AI.Samples.Common;
    using System.Collections.Generic;
    using WPC.AI.Samples.RssFeedAnalyzer.Model.Extensions;

    class Program
    {
        private static FeedDownloader m_FeedDownloader;
        private static WebScraper m_WebScraper;
        private static ContentAnalyzer m_ContentAnalyzer;

        public static async Task Main(string[] args)
        {
            WriteHeader();

            // Get /bin folder path
            // See: https://github.com/dotnet/project-system/issues/2239
            var executableFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Load configurable parameters from a json file
            // !! REMEMBER TO CONFIGURE YOUR CognitiveServices API key !!
            var configuration = LoadConfigurationFromJsonFile(Path.Combine(executableFolder, "appsettings.json"));

            // Initialize services and components. For simplicity, we do not use IoC/DI.
            SetupServices(configuration);

            // Setup a folder where to store analysis results
            string resultsPath = Path.Combine(executableFolder, "Results");
            if (!Directory.Exists(resultsPath))
            {
                Directory.CreateDirectory(resultsPath);
            }

            // Retrieve a list of feed items to analyze, either from online sources or from previous saved files
            IList<Model.FeedItem> feedItems = await GetFeedItems(args, resultsPath);

            // Analyze feed items
            foreach (var feedItem in feedItems)
            {
                var textAnalysisCollection = new List<Common.Model.TextAnalysisResult>();

                string destinationFile = $"{resultsPath}\\{feedItem.FeedId}_{feedItem.Id}_content.txt";

                // Load from existing file or download the link pointed by feed item and perform content clean-up (ad-hoc)
                Common.Model.WebPage webPage = await LoadFromFileAsync<Common.Model.WebPage>(destinationFile);
                if (webPage == null)
                {
                    webPage = await m_WebScraper.DownloadWebPageAsync(feedItem.Link);

                    if (webPage != null)
                    {
                        // Store web page content for later user
                        await StoreInFileAsync(webPage, destinationFile);
                    }
                }

                // Analyze the content summary in the feed item (if not already done)
                destinationFile = $"{resultsPath}\\{feedItem.FeedId}_{feedItem.Id}_summary_results.txt";
                Common.Model.TextAnalysisResult summaryAnalysisResult;
                if (!File.Exists(destinationFile))
                {
                    summaryAnalysisResult = await m_ContentAnalyzer.AnalyzeTextAsync(feedItem.Summary);
                    await StoreInFileAsync(summaryAnalysisResult, destinationFile);
                }
                else
                {
                    summaryAnalysisResult = await LoadFromFileAsync<Common.Model.TextAnalysisResult>(destinationFile);
                }

                textAnalysisCollection.Add(summaryAnalysisResult);

                if (webPage != null)
                {
                    // Analyze the content pointed by feed item (if not already done)
                    // Note: this analysis may not be necessary. It seems that pointed content analysis
                    //       is redundant compared to summary analysis. It may happen that summary is not
                    //       present, so analyzing content is the only option.
                    destinationFile = $"{resultsPath}\\{feedItem.FeedId}_{feedItem.Id}_content_results.txt";
                    Common.Model.TextAnalysisResult contentAnalysisResult;
                    if (!File.Exists(destinationFile))
                    {
                        contentAnalysisResult = await m_ContentAnalyzer.AnalyzeTextAsync(webPage.Text);
                        await StoreInFileAsync(contentAnalysisResult, destinationFile);
                    }
                    else
                    {
                        contentAnalysisResult = await LoadFromFileAsync<Common.Model.TextAnalysisResult>(destinationFile);
                    }

                    textAnalysisCollection.Add(contentAnalysisResult);
                }

                // Store normalized entities for later use
                var dataSetEntry = feedItem.MapToDataSetEntry(Common.Model.DataSetEntryType.RssItem, textAnalysisCollection);
                await StoreInFileAsync(dataSetEntry, $"{resultsPath}\\{feedItem.FeedId}_{feedItem.Id}_DataSetEntry.txt");
            }
        }

        private static async Task<IList<Model.FeedItem>> GetFeedItems(string[] args, string resultsPath)
        {
            // Try to load feed items from local file (if specified)
            string sourceFile = string.Empty;
            List<Model.FeedItem> feedItems = null;
            if (args.Length == 1)
            {
                feedItems = new List<Model.FeedItem>();
                var feedFiles = Directory.GetFiles(args[0], "*_feed.txt");
                foreach (var file in feedFiles)
                {
                    feedItems.AddRange(await LoadFromFileAsync<IList<Model.FeedItem>>(file));
                }
            }

            // Retrieve feed items from online sources
            if (feedItems == null)
            {
                // Configure some sample RSS Feeds to analyze
                var feedsToAnalyze = new List<Model.Feed> {
                new Model.Feed() { Id = "1", Name = "gianni's hub - RSS Feed EN", Url = "http://feeds.feedburner.com/giannishub" },
                new Model.Feed() { Id = "2", Name = "SkarredGhost", Url = "https://skarredghost.com/feed/" },
                new Model.Feed() { Id = "3", Name = "TechCrunch", Url = "http://feeds.feedburner.com/TechCrunch/" },
                new Model.Feed() { Id = "4", Name = "Mashable!", Url = "http://feeds.mashable.com/Mashable/" },
                new Model.Feed() { Id = "5", Name = "Microsoft", Url = "http://blogs.msdn.com/b/mainfeed.aspx?Type=BlogsOnly" },
                new Model.Feed() { Id = "6", Name = "Google Developers", Url = "http://feeds.feedburner.com/GDBcode/" },
                new Model.Feed() { Id = "7", Name = "Unity 3D", Url = "https://blogs.unity3d.com/feed" },
            };

                feedItems = new List<Model.FeedItem>();
                foreach (var feedToAnalyze in feedsToAnalyze)
                {
                    feedItems.AddRange(await m_FeedDownloader.DownloadRssFeedAsync(feedToAnalyze));
                    await StoreInFileAsync(feedItems, $"{resultsPath}\\{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}_{feedToAnalyze.Id}_feed.txt");
                }
            }

            return feedItems;
        }

        private static void SetupServices(IConfigurationRoot configuration)
        {
            m_FeedDownloader = new FeedDownloader();
            m_WebScraper = new WebScraper();
            m_ContentAnalyzer = new ContentAnalyzer(configuration["CognitiveServicesKeys:TextAnalyticsAPI"], configuration["CognitiveServicesKeys:VisionAPI"], configuration["CognitiveServicesKeys:VideoIndexerAPI"]);
        }

        private static IConfigurationRoot LoadConfigurationFromJsonFile(string file)
        {
            // See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration?tabs=basicconfiguration
            var builder = new ConfigurationBuilder().AddJsonFile(file);
            return builder.Build();
        }

        private static void WriteHeader()
        {
            Console.WriteLine("WPC 2017 - Microsoft AI Platform demo - RSS Feed Analyzer");
            Console.WriteLine("Copyright (C) 2017 Gianni Rosa Gallina. Released under MIT license.");
            Console.WriteLine("See LICENSE file for details.");
            Console.WriteLine("===================================================================");
        }

        private static async Task StoreInFileAsync<T>(T itemToStore, string destinationFile, bool overwrite = false)
        {
            if (overwrite || !File.Exists(destinationFile))
            {
                var serializedObject = JsonConvert.SerializeObject(itemToStore, Formatting.Indented);
                using (var writer = new StreamWriter(destinationFile))
                {
                    await writer.WriteAsync(serializedObject);
                }
            }
        }

        private static async Task<T> LoadFromFileAsync<T>(string sourceFile)
        {
            if (File.Exists(sourceFile))
            {
                using (var reader = new StreamReader(sourceFile))
                {
                    var serialized = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<T>(serialized);
                }
            }

            return default;
        }
    }
}
