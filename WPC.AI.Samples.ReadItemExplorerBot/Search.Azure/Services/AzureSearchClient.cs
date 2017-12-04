//
// Copyright (c) Gianni Rosa Gallina. All rights reserved.
// Licensed under the MIT license.
//
// Based on Microsoft Bot Builder Samples - Demo Search
// GitHub: https://github.com/Microsoft/BotBuilder-Samples.git
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
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

namespace Search.Azure.Services
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Search;
    using Microsoft.Azure.Search.Models;
    using Search.Models;
    using Search.Services;

    public class AzureSearchClient : ISearchClient
    {
        private readonly ISearchIndexClient searchClient;
        private readonly IMapper<DocumentSearchResult, GenericSearchResult> mapper;

        public AzureSearchClient(IMapper<DocumentSearchResult, GenericSearchResult> mapper)
        {
            this.mapper = mapper;
            SearchServiceClient client = new SearchServiceClient(
                ConfigurationManager.AppSettings["SearchDialogsServiceName"],
                new SearchCredentials(ConfigurationManager.AppSettings["SearchDialogsServiceKey"]));
            this.searchClient = client.Indexes.GetClient(ConfigurationManager.AppSettings["SearchDialogsIndexName"]);
        }

        public async Task<GenericSearchResult> SearchAsync(SearchQueryBuilder queryBuilder, string refiner)
        {
           var documentSearchResult = await this.searchClient.Documents.SearchAsync(queryBuilder.SearchText, BuildParameters(queryBuilder, refiner));

            return this.mapper.Map(documentSearchResult);
        }

        private static SearchParameters BuildParameters(SearchQueryBuilder queryBuilder, string facet)
        {
            SearchParameters parameters = new SearchParameters
            {
                Top = queryBuilder.HitsPerPage,
                Skip = queryBuilder.PageNumber * queryBuilder.HitsPerPage,
                SearchMode = SearchMode.All
            };

            if (facet != null)
            {
                parameters.Facets = new List<string> { facet };
            }

            if (queryBuilder.Refinements.Count > 0)
            {
                StringBuilder filter = new StringBuilder();
                string separator = string.Empty;

                foreach (var entry in queryBuilder.Refinements)
                {
                    foreach (string value in entry.Value)
                    {
                        filter.Append(separator);
                        filter.Append($"{entry.Key} lt {value}");
                        separator = " and ";
                    }
                }

                parameters.Filter = filter.ToString();
            }

            parameters.SearchFields = new[] { "Tags" };
            parameters.OrderBy = new[] { "FruitionTime desc", "search.score() desc" };

            return parameters;
        }

        private static string EscapeFilterString(string s)
        {
            return s.Replace("'", "''");
        }
    }
}
