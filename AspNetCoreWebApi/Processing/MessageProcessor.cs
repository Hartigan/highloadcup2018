using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreWebApi.Processing
{
    public class MessageProcessor
    {
        private readonly IDisposable _newAccountProcessorSubscription;
        private readonly IServiceProvider _serviceProvider;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            NewAccountProcessor newAccountProcessor)
        {
            _serviceProvider = serviceProvider;
            _newAccountProcessorSubscription = newAccountProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(AddNewAccounts);
        }

        private void AddNewAccounts(Tuple<Account, IEnumerable<Like>> data)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = _serviceProvider.GetRequiredService<AccountContext>())
                {
                    context.Accounts.Add(data.Item1);
                    context.Likes.AddRange(data.Item2);
                    context.SaveChanges();
                }
            }
        }
    }
}