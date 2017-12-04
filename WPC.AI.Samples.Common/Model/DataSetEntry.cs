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

namespace WPC.AI.Samples.Common.Model
{

    using System;
    using System.Collections.Generic;

    public class DataSetEntry
    {
        public string Id { get; set; }

        // Target variable
        public float Rank { get; set; }

        // For Azure Search Preview
        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }

        public string SourceUrl { get; set; }
        public DataSetEntryType EntryType { get; set; }
        public string Language { get; set; }
        public double SentimentScore { get; set; }
        public double AdultContentScore { get; set; }
        public double RacyContentScore { get; set; }
        public int ImagesCount { get; set; }
        public int PeopleTotalCount { get; set; }
        public int PeopleFemaleCount { get; set; }
        public int PeopleMaleCount { get; set; }
        public int VideosCount { get; set; }
        public double FruitionTime { get; set; }
        public string Category { get; set; }

        //public string DominantBackgroundColor { get; set; }
        //public string DominantForegroundColor { get; set; }
        //public string AccentColor { get; set; }
        //public bool IsBlackAndWhiteImage { get; set; }

        public IList<DataSetTag> Tags { get; }

        public DataSetEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Tags = new List<DataSetTag>();
            Language = "-";
            Category = "-";
            Rank = 0.0f;
            SentimentScore = -1.0;
            FruitionTime = 0;

            //DominantBackgroundColor = "-";
            //DominantForegroundColor = "-";
            //AccentColor = "-";
        }
    }
}
