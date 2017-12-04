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

namespace WPC.AI.Samples.ReadItemExplorerBot.Dialogs
{
    using System;
    using Microsoft.Bot.Builder.Dialogs;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector;

    [Serializable]
    public class TimeDialog : IDialog<string>
    {
        private int attempts = 3;

        public TimeDialog()
        {
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("How many minutes do you have to read something?");
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            /* If the message returned is a valid time in minutes, return it to the calling dialog. */
            double parsedTime;
            if ((message.Text != null) && (message.Text.Trim().Length > 0) && double.TryParse(message.Text, out parsedTime))
            {
                /* Completes the dialog, removes it from the dialog stack, and returns the result to the parent/calling dialog. */
                context.Done(message.Text);
            }
            /* Else, try again by re-prompting the user. */
            else
            {
                --attempts;
                if (attempts > 0)
                {
                    await context.PostAsync("I'm sorry, I don't understand your reply. How many minutes do you have to read something (e.g. '1', '2', '3.5', '5')?");

                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    /* Fails the current dialog, removes it from the dialog stack, and returns the exception to the  parent/calling dialog. */
                    context.Fail(new TooManyAttemptsException("Message was not a valid number or was an empty string."));
                }
            }
        }
    }
}
