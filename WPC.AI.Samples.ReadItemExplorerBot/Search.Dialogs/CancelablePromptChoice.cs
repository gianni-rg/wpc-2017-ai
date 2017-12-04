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
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;

    [Serializable]
    public class CancelablePromptChoice<T> : PromptDialog.PromptChoice<T>
    {
        protected readonly CancelablePromptOptions<T> PromptOptions;

        private static IEnumerable<string> cancelTerms = new[] { "Cancel", "Back", "B", "Abort" };

        public CancelablePromptChoice(CancelablePromptOptions<T> promptOptions)
            : base(promptOptions)
        {
            this.PromptOptions = promptOptions;
        }

        public CancelablePromptChoice(IEnumerable<T> options, string prompt, string cancelPrompt, string retry, int attempts, PromptStyle promptStyle = PromptStyle.Auto)
            : this(new CancelablePromptOptions<T>(prompt, cancelPrompt, retry, options: options.ToList(), attempts: attempts, promptStyler: new PromptStyler(promptStyle)))
        {
        }

        public static void Choice(IDialogContext context, ResumeAfter<T> resume, IEnumerable<T> options, string prompt, string cancelPrompt = null, string retry = null, int attempts = 3, PromptStyle promptStyle = PromptStyle.Auto)
        {
            Choice(context, resume, new CancelablePromptOptions<T>(prompt, cancelPrompt, retry, attempts: attempts, options: options.ToList(), promptStyler: new PromptStyler(promptStyle)));
        }

        public static void Choice(IDialogContext context, ResumeAfter<T> resume, CancelablePromptOptions<T> promptOptions)
        {
            var child = new CancelablePromptChoice<T>(promptOptions);
            context.Call(child, resume);
        }

        public static bool IsCancel(string text)
        {
            return cancelTerms.Any(t => string.Equals(t, text, StringComparison.CurrentCultureIgnoreCase));
        }

        protected override bool TryParse(IMessageActivity message, out T result)
        {
            if (IsCancel(message.Text))
            {
                result = default(T);
                return true;
            }

            return base.TryParse(message, out result);
        }

        protected override IMessageActivity MakePrompt(IDialogContext context, string prompt, IReadOnlyList<T> options = null, IReadOnlyList<string> descriptions = null, string speak = null)
        {
            prompt += Environment.NewLine + (this.PromptOptions.CancelPrompt ?? this.PromptOptions.DefaultCancelPrompt);
            return base.MakePrompt(context, prompt, options, descriptions, speak);
        }
    }
}