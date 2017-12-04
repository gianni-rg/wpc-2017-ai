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

namespace Search.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Internals.Fibers;
    using Search.Models;
    using Search.Services;

    [Serializable]
    public class SearchRefineDialog : IDialog<string>
    {
        protected readonly string Refiner;
        protected readonly SearchQueryBuilder QueryBuilder;
        protected readonly PromptStyler PromptStyler;
        protected readonly string Prompt;
        protected readonly ISearchClient SearchClient;

        public SearchRefineDialog(ISearchClient searchClient, string refiner, SearchQueryBuilder queryBuilder = null, PromptStyler promptStyler = null, string prompt = null)
        {
            SetField.NotNull(out this.SearchClient, nameof(searchClient), searchClient);
            SetField.NotNull(out this.Refiner, nameof(refiner), refiner);

            this.QueryBuilder = queryBuilder ?? new SearchQueryBuilder();
            this.PromptStyler = promptStyler;
            this.Prompt = prompt ?? $"Here's what I found for {this.Refiner}.";
        }

        public async Task StartAsync(IDialogContext context)
        {
            var result = await this.SearchClient.SearchAsync(this.QueryBuilder, this.Refiner);

            IEnumerable<string> options = result.Facets[this.Refiner].Select(f => this.FormatRefinerOption((string)f.Value, f.Count));

            var promptOptions = new CancelablePromptOptions<string>(this.Prompt, cancelPrompt: "Type 'cancel' if you don't want to select any of these.", options: options.ToList(), promptStyler: this.PromptStyler);
            CancelablePromptChoice<string>.Choice(context, this.ApplyRefiner, promptOptions);
        }

        public async Task ApplyRefiner(IDialogContext context, IAwaitable<string> input)
        {
            string selection = await input;

            if (selection == null)
            {
                context.Done<string>(null);
            }
            else
            {
                string value = this.ParseRefinerValue(selection);

                if (this.QueryBuilder != null)
                {
                    await context.PostAsync($"Filtering by {this.Refiner}: {value}");
                    this.QueryBuilder.Refinements.Add(this.Refiner, new string[] { value });
                }

                context.Done(value);
            }
        }

        protected virtual string FormatRefinerOption(string value, long count)
        {
            return $"{value} ({count})";
        }

        protected virtual string ParseRefinerValue(string value)
        {
            return value.Substring(0, value.LastIndexOf('(') - 1);
        }
    }
}
