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

namespace WPC.AI.Samples.RssFeedAnalyzer.Model.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using WPC.AI.Samples.Common.Model;

    public static class Mappers
    {
        public static DataSetEntry MapToDataSetEntry(this FeedItem feedItem, DataSetEntryType entryType, IList<TextAnalysisResult> textAnalysisResult = null, IList<ImageAnalysisResult> imageAnalysisResult = null, IList<VideoAnalysisResult> videoAnalysisResult = null)
        {
            var dsEntry = new DataSetEntry();
            dsEntry.EntryType = entryType;
            dsEntry.SourceUrl = feedItem.Link;
            dsEntry.FruitionTime = 0;
            dsEntry.Title = feedItem.Title;
            //dsEntry.ThumbnailUrl = feedItem.ThumbnailUrl;

            double maxLanguageScore = 0.0;
            double maxSentimentScore = 0.0;
            if (textAnalysisResult != null)
            {
                foreach (var textAnalysis in textAnalysisResult)
                {
                    if (textAnalysis == null)
                    {
                        continue;
                    }

                    // Set the entry language to the most confident detected language score
                    if (textAnalysis.DetectedLanguageScore > maxLanguageScore)
                    {
                        maxLanguageScore = textAnalysis.DetectedLanguageScore;
                        dsEntry.Language = textAnalysis.DetectedLanguage;
                    }

                    // Set the entry sentiment to the most confident detected sentiment score
                    if (textAnalysis.SentimentScore > maxSentimentScore)
                    {
                        maxSentimentScore = textAnalysis.SentimentScore;
                        dsEntry.SentimentScore = textAnalysis.SentimentScore;
                    }

                    // Retrieve and combine all detected tags
                    foreach (var keyPhrase in textAnalysis.KeyPhrases)
                    {
                        dsEntry.Tags.Add(new DataSetTag { Name = keyPhrase, Score = 0.0 });
                    }

                    // Set the total fruition time to the sum of all text reading times
                    dsEntry.FruitionTime += textAnalysis.ReadingTimeInMinutes;
                }
            }

            if (imageAnalysisResult != null)
            {
                dsEntry.ImagesCount = imageAnalysisResult.Count;

                double maxRacyScore = 0.0;
                double maxAdultScore = 0.0;
                foreach (var imageAnalysis in imageAnalysisResult)
                {
                    if (imageAnalysis == null)
                    {
                        continue;
                    }

                    // Retrieve and combine all detected categories
                    foreach (var category in imageAnalysis.Categories)
                    {
                        dsEntry.Tags.Add(new DataSetTag { Name = category.Text, Score = category.Score });
                    }

                    // Set the entry racy score to the most confident detected racy score
                    if (imageAnalysis.AdultContent.RacyScore > maxRacyScore)
                    {
                        maxRacyScore = imageAnalysis.AdultContent.RacyScore;
                        dsEntry.RacyContentScore = imageAnalysis.AdultContent.RacyScore;
                    }

                    // Set the entry adult score to the most confident detected adult score
                    if (imageAnalysis.AdultContent.AdultScore > maxAdultScore)
                    {
                        maxAdultScore = imageAnalysis.AdultContent.AdultScore;
                        dsEntry.AdultContentScore = imageAnalysis.AdultContent.AdultScore;
                    }

                    // Sum the number of people found in all images
                    dsEntry.PeopleTotalCount += imageAnalysis.Faces.Count;
                    dsEntry.PeopleFemaleCount += imageAnalysis.Faces.Where(f => f.Gender == Gender.Female).Count();
                    dsEntry.PeopleMaleCount += imageAnalysis.Faces.Where(f => f.Gender == Gender.Male).Count();

                    // Set the total fruition time to the sum of all image watching times
                    dsEntry.FruitionTime += imageAnalysis.WatchingTimeInMinutes;

                    // Ignore colors. How to use them?
                    //dsEntry.IsBlackAndWhiteImage = imageAnalysis.Colors.IsBWImg;
                    //dsEntry.DominantBackgroundColor = imageAnalysis.Colors.DominantColorBackground;
                    //dsEntry.DominantForegroundColor = imageAnalysis.Colors.DominantColorForeground;
                    //dsEntry.AccentColor = imageAnalysis.Colors.AccentColor;
                }
            }

            if (videoAnalysisResult != null)
            {
                dsEntry.VideosCount = videoAnalysisResult.Count;

                foreach (var videoAnalysis in videoAnalysisResult)
                {
                    if (videoAnalysis == null)
                    {
                        continue;
                    }

                    // Set the total fruition time to the sum of all video length times
                    dsEntry.FruitionTime += videoAnalysis.LengthInMinutes;

                    // Sum the number of people found in all videos
                    //dsEntry.PeopleTotalCount += videoAnalysis.Faces.Count;
                    //dsEntry.PeopleFemaleCount += videoAnalysis.Faces.Where(f => f.Gender == Gender.Female).Count();
                    //dsEntry.PeopleMaleCount += videoAnalysis.Faces.Where(f => f.Gender == Gender.Male).Count();
                }
            }

            return dsEntry;
        }
    }
}
