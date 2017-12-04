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

    [Serializable]
    public class SearchSelectRefinerDialog : IDialog<string>
    {
        protected readonly SearchQueryBuilder QueryBuilder;
        protected readonly IEnumerable<string> Refiners;
        protected readonly PromptStyler PromptStyler;

        public SearchSelectRefinerDialog(IEnumerable<string> refiners, SearchQueryBuilder queryBuilder = null, PromptStyler promptStyler = null)
        {
            SetField.NotNull(out this.Refiners, nameof(refiners), refiners);

            this.QueryBuilder = queryBuilder ?? new SearchQueryBuilder();
            this.PromptStyler = promptStyler;
        }

        public async Task StartAsync(IDialogContext context)
        {
            IEnumerable<string> unusedRefiners = this.Refiners;
            if (this.QueryBuilder != null)
            {
                unusedRefiners = unusedRefiners.Except(this.QueryBuilder.Refinements.Keys, StringComparer.OrdinalIgnoreCase);
            }

            if (unusedRefiners.Any())
            {
                var promptOptions = new CancelablePromptOptions<string>("What do you want to refine by?", cancelPrompt: "Type 'cancel' if you changed your mind.", options: unusedRefiners.ToList(), promptStyler: this.PromptStyler);
                CancelablePromptChoice<string>.Choice(context, this.ReturnSelection, promptOptions);
            }
            else
            {
                await context.PostAsync("Oops! You used all the available refiners and you cannot refine the results anymore.");
                context.Done<string>(null);
            }
        }

        protected virtual async Task ReturnSelection(IDialogContext context, IAwaitable<string> input)
        {
            var selection = await input;

            context.Done(selection);
        }
    }
}
