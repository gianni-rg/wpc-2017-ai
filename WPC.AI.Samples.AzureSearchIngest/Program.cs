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

namespace WPC.AI.Samples.AzureSearchIngest
{
    using Microsoft.Azure.Search;
    using Microsoft.Azure.Search.Models;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using WPC.AI.Samples.AzureSearchIngest.Model;
    using WPC.AI.Samples.AzureSearchIngest.Model.Extensions;
    using WPC.AI.Samples.Common.Model;

    class Program
    {
        private static ISearchServiceClient m_SearchClient;
        private static ISearchIndexClient m_IndexClient;
        private static string[] m_InputFolders;
        private static string m_ItemsIndexName;

        static async Task Main(string[] args)
        {
            WriteHeader();

            // Get /bin folder path
            // See: https://github.com/dotnet/project-system/issues/2239
            var executableFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Load configurable parameters from a json file
            // !! REMEMBER TO CONFIGURE YOUR AzureSearch API settings !!
            var configuration = LoadConfigurationFromJsonFile(Path.Combine(executableFolder, "appsettings.json"));

            // Setup folders where to read/store data
            SetupFolders(configuration, executableFolder);

            // Initialize services and components. For simplicity, we do not use IoC/DI.
            SetupServices(configuration);

            Console.WriteLine("Deleting items index (if exists)");
            if (await DeleteItemsIndexIfExistsAsync())
            {
                Console.WriteLine("Creating items index");
                await CreateItemsIndexAsync();

                Console.WriteLine("Ingesting documents from local files");
                await UploadDataFromLocalFilesAsync();
            }

            await TestQueriesAsync(1, "star wars");

            Console.WriteLine("Complete. Press any key to terminate...\n");
            Console.ReadLine();
        }

        private async static Task TestQueriesAsync(double fruitionTime, string topic)
        {
            // See: https://docs.microsoft.com/en-us/rest/api/searchservice/odata-expression-syntax-for-azure-search

            SearchParameters parameters;
            DocumentSearchResult<AzureSearchDoc> results;

            // Search the entire index for items which fruitionTime is less than 3mins and topics is technology
            // ordering by fruitionTime
            
            parameters =
                new SearchParameters()
                {
                    QueryType = QueryType.Full,
                    SearchFields = new[] { "Tags" },
                    Filter = $"FruitionTime lt {fruitionTime}",
                    OrderBy = new[] { "FruitionTime desc", "search.score() desc" },
                    //Select = new[] { "Id", "SourceUrl" },
                    Top = 30,
                };

            results = await m_IndexClient.Documents.SearchAsync<AzureSearchDoc>(topic, parameters);
            foreach(var r in results.Results)
            {
                Console.WriteLine($"({r.Document.FruitionTime:0.0}min) {r.Document.SourceUrl} ({r.Score})");
            }
        }

        private async static Task UploadDataFromLocalFilesAsync()
        {
            // Retrieve all DataSetEntries from Twitter and RSS Feed analyzers
            var allDataSetEntryFiles = new List<string>();
            foreach (var inputFolder in m_InputFolders)
            {
                allDataSetEntryFiles.AddRange(Directory.GetFiles(inputFolder, "*_DataSetEntry.txt"));
            }

            // Split items upload in batch of up to 1000 records
            int maxRecordsPerUpload = 1000;
            bool doUpload = false;
            IList<AzureSearchDoc> items = new List<AzureSearchDoc>();
            foreach(var entryFile in allDataSetEntryFiles)
            {
                if (doUpload)
                {
                    await PerformUploadAsync(items);
                    items.Clear();
                    doUpload = false;
                }
                
                using (var reader = new StreamReader(entryFile))
                {
                    var serializedEntry = await reader.ReadToEndAsync();

                    // Map DataSetEntry to AzureDoc entity
                    items.Add(JsonConvert.DeserializeObject<DataSetEntry>(serializedEntry).ToAzureSearchDoc());
                }
                
                if (items.Count == allDataSetEntryFiles.Count || items.Count >= maxRecordsPerUpload)
                {
                    doUpload = true;
                }
            }

            // Last items to upload
            if (items.Count > 0)
            {
                await PerformUploadAsync(items);
            }

            Console.WriteLine("Upload completed");
        }

        private async static Task PerformUploadAsync(IList<AzureSearchDoc> items)
        {
            if(items == null || items.Count == 0)
            {
                return;
            }

            var batch = IndexBatch.Upload(items);

            try
            {
                await m_IndexClient.Documents.IndexAsync(batch);
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine("Failed to index some of the documents: {0}", string.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }
        }

        private async static Task CreateItemsIndexAsync()
        {
            // Create the Azure Search index based on the included schema
            try
            {
                var definition = new Index()
                {
                    Name = m_ItemsIndexName,
                    Fields = new[]
                    {
                        new Field("Id",                 DataType.String)                      { IsKey = true,  IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("Tags",               DataType.Collection(DataType.String)) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = false,  IsFacetable = false, IsRetrievable = true},
                        new Field("SourceUrl",          DataType.String)                      { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("Title",              DataType.String)                      { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("ThumbnailUrl",       DataType.String)                      { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("Description",        DataType.String)                      { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("EntryType",          DataType.String)                      { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("Language",           DataType.String)                      { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                        new Field("FruitionTime",       DataType.Double)                      { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true},
                        new Field("Category",           DataType.String)                      { IsKey = false, IsSearchable = true,  IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
                    }
                };

                await m_SearchClient.Indexes.CreateAsync(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}", ex.Message);
            }
        }

        private async static Task<bool> DeleteItemsIndexIfExistsAsync()
        {
            try
            {
                await m_SearchClient.Indexes.DeleteAsync(m_ItemsIndexName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting indexing resources: {0}", ex.Message);
                return false;
            }

            return true;
        }

        private static void SetupFolders(IConfigurationRoot configuration, string executableFolder)
        {
            bool inputRelativePath = bool.Parse(configuration["Folders:InputRelativePaths"]);

            m_InputFolders = configuration.GetSection("Folders:InputFolders").AsEnumerable().Where(i => !string.IsNullOrEmpty(i.Value)).Select(i => i.Value).ToArray();

            if (inputRelativePath)
            {
                for (int i = 0; i < m_InputFolders.Length; i++)
                {
                    m_InputFolders[i] = Path.Combine(executableFolder, m_InputFolders[i]);
                }
            }
        }

        private static void SetupServices(IConfigurationRoot configuration)
        {
            m_ItemsIndexName = configuration["AzureSearch:SearchIndexName"];

            m_SearchClient = new SearchServiceClient(configuration["AzureSearch:SearchServiceName"], new SearchCredentials(configuration["AzureSearch:SearchServiceApiKey"]));
            m_IndexClient = m_SearchClient.Indexes.GetClient(m_ItemsIndexName);

        }

        private static IConfigurationRoot LoadConfigurationFromJsonFile(string file)
        {
            // See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration?tabs=basicconfiguration
            var builder = new ConfigurationBuilder().AddJsonFile(file);
            return builder.Build();
        }

        private static void WriteHeader()
        {
            Console.WriteLine("WPC 2017 - Microsoft AI Platform demo - Azure Search Ingester");
            Console.WriteLine("Copyright (C) 2017 Gianni Rosa Gallina. Released under MIT license.");
            Console.WriteLine("See LICENSE file for details.");
            Console.WriteLine("===================================================================");
        }
    }
}
