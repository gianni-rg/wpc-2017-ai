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

namespace WPC.AI.Samples.ReadItemExplorerBot
{
    using System.Linq;
    using Microsoft.Azure.Search.Models;
    using Search.Azure.Services;
    using Search.Models;

    public class DataSetEntryMapper : IMapper<DocumentSearchResult, GenericSearchResult>
    {
        public GenericSearchResult Map(DocumentSearchResult documentSearchResult)
        {
            var searchResult = new GenericSearchResult();

            searchResult.Results = documentSearchResult.Results.Select(r => ToSearchHit(r)).ToList();
            searchResult.Facets = documentSearchResult.Facets?.ToDictionary(kv => kv.Key, kv => kv.Value.Select(f => ToFacet(f)));

            return searchResult;
        }

        private static GenericFacet ToFacet(FacetResult facetResult)
        {
            return new GenericFacet
            {
                Value = facetResult.Value,
                Count = facetResult.Count.Value
            };
        }

        private static SearchHit ToSearchHit(SearchResult hit)
        {
            return new SearchHit
            {
                Key = (string)hit.Document["Id"],
                Title = (string)hit.Document["Title"],
                SourceUrl = (string)hit.Document["SourceUrl"],
                Description = GetDescriptionForItem(hit),
                FruitionTime = (double)hit.Document["FruitionTime"],
            };
        }

        private static string GetDescriptionForItem(SearchResult result)
        {
            var fruitionTime = result.Document["FruitionTime"];
            return $"Reading time: {fruitionTime:0.0} mins";
        }
    }
}
