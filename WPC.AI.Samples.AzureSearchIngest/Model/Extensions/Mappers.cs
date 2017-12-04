﻿//
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

namespace WPC.AI.Samples.AzureSearchIngest.Model.Extensions
{
    using System.Globalization;
    using WPC.AI.Samples.AzureSearchIngest.Model;
    using WPC.AI.Samples.Common.Model;

    public static class Mappers
    {
        private static CultureInfo m_EnglishCulture = new CultureInfo("en-US");

        public static AzureSearchDoc ToAzureSearchDoc(this DataSetEntry dsEntry)
        {
            var doc = new AzureSearchDoc()
            {
                Id = dsEntry.Id,
                Title = dsEntry.Title,
                ThumbnailUrl = dsEntry.ThumbnailUrl,
                Description = null,
                Category = dsEntry.Category,
                EntryType = dsEntry.EntryType.ToString(),
                FruitionTime = dsEntry.FruitionTime,
                Language = dsEntry.Language,
                SourceUrl = dsEntry.SourceUrl,
            };

            foreach (var tag in dsEntry.Tags)
            {
                doc.Tags.Add(tag.Name);
            }
            return doc;
        }
    }
}
