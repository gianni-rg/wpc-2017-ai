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

namespace WPC.AI.Samples.ReadItemExplorerBot
{
    using Autofac;
    using Microsoft.Azure.Search.Models;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Internals.Fibers;
    using Search.Azure.Services;
    using Search.Models;
    using Search.Services;
    using System.Web.Http;
    using WPC.AI.Samples.ReadItemExplorerBot.Dialogs;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Conversation.UpdateContainer(builder =>
            {
                builder.RegisterType<IntroDialog>()
                  .As<IDialog<object>>()
                  .InstancePerDependency();

                builder.RegisterType<DataSetEntryMapper>()
                   .Keyed<IMapper<DocumentSearchResult, GenericSearchResult>>(FiberModule.Key_DoNotSerialize)
                   .AsImplementedInterfaces()
                   .SingleInstance();

                builder.RegisterType<AzureSearchClient>()
                    .Keyed<ISearchClient>(FiberModule.Key_DoNotSerialize)
                    .AsImplementedInterfaces()
                    .SingleInstance();
            });

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
