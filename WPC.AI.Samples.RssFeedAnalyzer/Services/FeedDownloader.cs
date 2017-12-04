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

namespace WPC.AI.Samples.RssFeedAnalyzer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CodeHollow.FeedReader;
    using WPC.AI.Samples.Common.Services.Helpers;
    using System.Security.Cryptography;
    using System.Text;

    public class FeedDownloader
    {
        private HTMLStripper m_HtmlStripper;
        private SHA256 m_EntityHasher;
        private UTF8Encoding m_Utf8Encoding;

        public FeedDownloader()
        {
            m_HtmlStripper = new HTMLStripper();

            m_EntityHasher = new SHA256Managed();
            m_EntityHasher.Initialize();
            m_Utf8Encoding = new UTF8Encoding();
        }

        public Task<IList<Model.FeedItem>> DownloadRssFeedAsync(Model.Feed feed)
        {
            return Task.Run(async () =>
            {
                IList<Model.FeedItem> feedItems = new List<Model.FeedItem>();

                var rssFeed = await FeedReader.ReadAsync(feed.Url);

                // TO ACCESS CUSTOM FEED METADATA BY RSS FEED VERSION
                // NOT USED AT THE MOMENT
                if (rssFeed.Type == FeedType.Rss_2_0)
                {
                    var rss20feed = (CodeHollow.FeedReader.Feeds.Rss20Feed)rssFeed.SpecificFeed;
                }

                Console.WriteLine($"FeedDownloader.DownloadRssFeedAsync(): loading RSS feed from {feed.Url}");

                foreach (var item in rssFeed.Items)
                {
                    var newItem = MapSyndicationFeedItemToFeedItem(item, rssFeed.Type);
                    if (newItem != null)
                    {
                        newItem.FeedId = feed.Id;
                        feedItems.Add(newItem);
                    }
                }

                Console.WriteLine($"FeedDownloader.DownloadRssFeedAsync(): retrieved {feedItems.Count} feed items");

                return feedItems;
            });
        }

        private Model.FeedItem MapSyndicationFeedItemToFeedItem(FeedItem item, FeedType feedType)
        {
            bool valid = true;

            if (item == null)
            {
                Console.WriteLine("FeedDownloader.MapSyndicationFeedItemToFeedItem(): item is null");

                valid = false;
            }

            if (valid && string.IsNullOrEmpty(item.Title))
            {
                Console.WriteLine("FeedDownloader.MapSyndicationFeedItemToFeedItem(): item is not valid (No title)");
                valid = false;
            }

            //if (valid && string.IsNullOrEmpty(item.Description))
            //{
            //    Console.WriteLine($"FeedDownloader.MapSyndicationFeedItemToFeedItem(): item '{item.Title}' is not valid (No summary)");
            //    valid = false;
            //}

            if (valid && string.IsNullOrEmpty(item.Link))
            {
                Console.WriteLine($"FeedDownloader.MapSyndicationFeedItemToFeedItem(): item '{item.Title}' is not valid (No link)");
                valid = false;
            }

            if (!valid)
            {
                Console.WriteLine("FeedDownloader.MapSyndicationFeedItemToFeedItem(): found invalid item. Ignore.");
                return null;
            }

            var feedItem = new Model.FeedItem
            {
                Title = m_HtmlStripper.UnHtml(item.Title),

                Link = item.Link,
                PublishDate = item.PublishingDate.Value.ToUniversalTime(),

                // Convert Summary Text to plain text (if any HTML tag is present)
                Summary = m_HtmlStripper.UnHtml(item.Description)
            };
            feedItem.Id = GetFeedItemHash(feedItem);

            // TO ACCESS CUSTOM ITEM METADATA BY RSS FEED VERSION
            // NOT USED AT THE MOMENT
            if (feedType == FeedType.Rss_2_0)
            {
                var rss20feedItem = (CodeHollow.FeedReader.Feeds.Rss20FeedItem)item.SpecificItem;
            }

            return feedItem;
        }

        private string GetFeedItemHash(Model.FeedItem feedItem)
        {
            return BitConverter.ToString(m_EntityHasher.ComputeHash(m_Utf8Encoding.GetBytes($"{feedItem.Title}_{feedItem.Summary}_{feedItem.Link}"))).Replace("-", "");
        }
    }
}
