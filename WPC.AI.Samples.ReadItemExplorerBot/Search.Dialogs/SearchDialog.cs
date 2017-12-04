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
    using Microsoft.Bot.Connector;
    using Search.Models;
    using Search.Services;
    using WPC.AI.Samples.ReadItemExplorerBot.Dialogs;

    [Serializable]
    public abstract class SearchDialog : IDialog<IList<SearchHit>>
    {
        protected readonly ISearchClient SearchClient;
        protected readonly SearchQueryBuilder QueryBuilder;
        protected readonly PromptStyler HitStyler;
        protected readonly bool MultipleSelection;
        private readonly IList<SearchHit> selected = new List<SearchHit>();

        private bool firstPrompt = true;
        private IList<SearchHit> found;

        public SearchDialog(ISearchClient searchClient, SearchQueryBuilder queryBuilder = null, PromptStyler searchHitStyler = null, bool multipleSelection = false)
        {
            SetField.NotNull(out this.SearchClient, nameof(searchClient), searchClient);

            this.QueryBuilder = queryBuilder ?? new SearchQueryBuilder();
            this.HitStyler = searchHitStyler ?? new SearchHitStyler();
            this.MultipleSelection = multipleSelection;
        }

        public Task StartAsync(IDialogContext context)
        {
            return this.InitialPrompt(context);
        }

        public async Task Search(IDialogContext context, IAwaitable<string> input)
        {
            string text = input != null ? await input : null;
            if (this.MultipleSelection && text != null && text.ToLowerInvariant() == "list")
            {
                await this.ListAddedSoFar(context);
                await this.InitialPrompt(context);
            }
            else
            {
                if (text != null)
                {
                    this.QueryBuilder.SearchText = text;
                }

                var response = await this.ExecuteSearchAsync();

                if (response.Results.Count() == 0)
                {
                    await this.NoResultsConfirmRetry(context);
                }
                else
                {
                    var message = context.MakeMessage();
                    this.found = response.Results.ToList();
                    this.HitStyler.Apply(
                        ref message,
                        "Here are a few good options I found:",
                        this.found.ToList().AsReadOnly());
                    await context.PostAsync(message);
                    await context.PostAsync(
                        this.MultipleSelection ?
                        "You can select one or more to add to your list, *list* what you've selected so far, see *more* or search *again*." :
                        "You can select one, see *more* or search *again*.");
                    context.Wait(this.ActOnSearchResults);
                }
            }
        }

        protected async virtual Task InitialPrompt(IDialogContext context, double remainingTime = double.MaxValue)
        {
            string prompt = "Hello. I'm here to help you find something interesting to read in your limited time.";

            if (remainingTime <= 0.3)
            {
                prompt = "You have no more time available.";
                await context.PostAsync(prompt);
                context.Done(this.selected);
                return;
            }

            if (remainingTime < double.MaxValue)
            {
                await ChooseWhatToSearchFor(context, remainingTime.ToString("0.0"));
                return;
            }

            if (!this.firstPrompt)
            {
                context.Call(new TimeDialog(), ResumeFromGetTime);
                return;
            }

            this.firstPrompt = false;
            await context.PostAsync(prompt);

            context.Call(new TimeDialog(), ResumeFromGetTime);
        }

        protected virtual Task ChooseWhatToSearchFor(IDialogContext context, string fruitionTime)
        {
            string prompt = $"What would you like to read about in the next {fruitionTime} minutes?";
            PromptDialog.Text(context, this.Search, prompt);
            return Task.CompletedTask;
        }

        protected virtual Task NoResultsConfirmRetry(IDialogContext context)
        {
            PromptDialog.Confirm(context, this.ShouldRetry, "Sorry, I didn't find any matches. Do you want to retry?");
            return Task.CompletedTask;
        }

        protected virtual async Task ListAddedSoFar(IDialogContext context)
        {
            var message = context.MakeMessage();
            if (this.selected.Count == 0)
            {
                await context.PostAsync("You have not anything to read yet.");
            }
            else
            {
                this.HitStyler.Apply(ref message, "Here's what you pick to read so far.", this.selected.ToList().AsReadOnly());
                await context.PostAsync(message);
            }
        }

        protected virtual async Task AddSelectedItem(IDialogContext context, string selection)
        {
            SearchHit hit = this.found.SingleOrDefault(h => h.Key == selection);
            if (hit == null)
            {
                await this.UnkownActionOnResults(context, selection);
            }
            else
            {
                if (!this.selected.Any(h => h.Key == hit.Key))
                {
                    this.selected.Add(hit);
                }

                if (this.MultipleSelection)
                {
                    await context.PostAsync($"'{hit.SourceUrl}' was added to your list!");
                    PromptDialog.Confirm(context, this.ShouldContinueSearching, "Do you want to continue searching and adding more items?");
                }
                else
                {
                    context.Done(this.selected);
                }
            }
        }

        protected virtual async Task UnkownActionOnResults(IDialogContext context, string action)
        {
            await context.PostAsync("Not sure what you mean. You can search *again*, *list* or select one of the items above. Or are you *done*?");
            context.Wait(this.ActOnSearchResults);
        }

        protected virtual async Task ShouldContinueSearching(IDialogContext context, IAwaitable<bool> input)
        {
            try
            {
                bool shouldContinue = await input;
                if (shouldContinue)
                {
                    double remainingTime = double.MaxValue;
                    try
                    {
                        double totalReadingTimeSoFar = 0;
                        foreach (var selectedItem in selected)
                        {
                            totalReadingTimeSoFar += selectedItem.FruitionTime;
                        }

                        if (context.UserData.ContainsKey("TotalFruitionTime"))
                        {
                            var totalAvailableTime = context.UserData.GetValue<double>("TotalFruitionTime");
                            remainingTime = totalAvailableTime - totalReadingTimeSoFar;
                        }

                        if (this.QueryBuilder.Refinements.ContainsKey("FruitionTime"))
                        {
                            this.QueryBuilder.Refinements["FruitionTime"] = new string[] { remainingTime.ToString() };
                        }
                        else
                        {
                            this.QueryBuilder.Refinements.Add("FruitionTime", new string[] { remainingTime.ToString() });
                        }
                    }
                    catch (Exception)
                    {
                    }

                    await this.InitialPrompt(context, remainingTime);
                }
                else
                {
                    context.Done(this.selected);
                }
            }
            catch (TooManyAttemptsException)
            {
                context.Done(this.selected);
            }
        }
        
        protected async Task ResumeFromGetTime(IDialogContext context, IAwaitable<string> input)
        {
            // apply duration filter to query
            string selection = await input;

            if (selection == null)
            {
                context.Done<string>(null);
            }
            else
            {
                if (double.TryParse(selection, out double tempVal))
                {
                    context.UserData.SetValue("TotalFruitionTime", tempVal);
                }

                if (this.QueryBuilder != null)
                {
                    if (!this.QueryBuilder.Refinements.ContainsKey("FruitionTime"))
                    {
                        this.QueryBuilder.Refinements.Add("FruitionTime", new string[] { selection });
                    }
                    else
                    {
                        this.QueryBuilder.Refinements["FruitionTime"] = new string[] { selection };
                    }
                }

                await ChooseWhatToSearchFor(context, selection);
            }
        }

        protected async Task<GenericSearchResult> ExecuteSearchAsync()
        {
            return await this.SearchClient.SearchAsync(this.QueryBuilder);
        }

        protected abstract string[] GetTopRefiners();

        private async Task ShouldRetry(IDialogContext context, IAwaitable<bool> input)
        {
            try
            {
                bool retry = await input;
                if (retry)
                {
                    double availableTime = double.MaxValue;
                    if (this.QueryBuilder.Refinements.ContainsKey("FruitionTime"))
                    {
                        availableTime = double.Parse(QueryBuilder.Refinements["FruitionTime"].First().ToString());
                    }
                    await this.InitialPrompt(context, availableTime);
                }
                else
                {
                    if (this.selected != null && this.selected.Count > 0)
                    {
                        context.Done(this.selected);
                    }
                    else
                    {
                        context.Done<IList<SearchHit>>(null);
                    }
                }
            }
            catch (TooManyAttemptsException)
            {
                context.Done<IList<SearchHit>>(null);
            }
        }

        private async Task ActOnSearchResults(IDialogContext context, IAwaitable<IMessageActivity> input)
        {
            var activity = await input;
            var choice = activity.Text;

            switch (choice.ToLowerInvariant())
            {
                case "again":
                case "reset":
                case "yes":

                    if (context.UserData.ContainsKey("TotalFruitionTime"))
                    {
                        context.UserData.RemoveValue("TotalFruitionTime");
                    }

                    this.QueryBuilder.Reset();
                    await this.InitialPrompt(context);
                    break;

                case "more":
                    this.QueryBuilder.PageNumber++;
                    await this.Search(context, null);
                    break;

                case "list":
                    await this.ListAddedSoFar(context);
                    context.Wait(this.ActOnSearchResults);
                    break;

                case "no":
                case "done":
                    context.Done(this.selected);
                    break;

                default:
                    await this.AddSelectedItem(context, choice);
                    break;
            }
        }
    }
}
