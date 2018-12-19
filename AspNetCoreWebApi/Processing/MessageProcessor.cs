using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreWebApi.Processing
{
    public class MessageProcessor
    {
        private readonly IDisposable _newAccountProcessorSubscription;
        private readonly IDisposable _editAccountProcessorSubscription;
        private readonly IDisposable _newLikesProcessorSubscription;
        private readonly IServiceProvider _serviceProvider;
        private readonly CountryStorage _countryStorage;
        private readonly CityStorage _cityStorage;
        private readonly InterestStorage _interestStorage;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            CountryStorage countryStorage,
            CityStorage cityStorage,
            InterestStorage interestStorage,
            NewAccountProcessor newAccountProcessor,
            EditAccountProcessor editAccountProcessor,
            NewLikesProcessor newLikesProcessor)
        {
            _serviceProvider = serviceProvider;
            _countryStorage = countryStorage;
            _cityStorage = cityStorage;
            _interestStorage = interestStorage;

            _newAccountProcessorSubscription = newAccountProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(AddNewAccount);

            _editAccountProcessorSubscription = editAccountProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(EditAccount);

            _newLikesProcessorSubscription = newLikesProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(NewLikes);
        }

        private void EditAccount(Tuple<int, AccountDto> data)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = services.GetRequiredService<AccountContext>())
                {
                    Account account = context.Accounts.Single(x => x.Id == data.Item1);

                    AccountDto dto = data.Item2;

                    if (dto.Email != null) account.Email = dto.Email;
                    if (dto.FirstName != null) account.FirstName = dto.FirstName;
                    if (dto.Surname != null) account.LastName = dto.Surname;
                    if (dto.Phone != null) account.Phone = dto.Phone;
                    if (dto.Birth != null) account.Birth = DateTimeOffset.FromUnixTimeSeconds(dto.Birth.Value);
                    if (dto.Country != null) account.CountryId = _countryStorage.Get(dto.Country);
                    if (dto.City != null) account.CityId = _cityStorage.Get(dto.City);
                    if (dto.Joined != null) account.Joined = DateTimeOffset.FromUnixTimeSeconds(dto.Joined.Value);
                    if (dto.Interests != null) account.Interests = dto.Interests.Select(x => new Interest() { StringId = _interestStorage.Get(x) }).ToList();
                    if (dto.Premium != null)
                    {
                        account.PremiumStart = DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Start);
                        account.PremiumEnd = DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Finish);
                    }

                    context.Accounts.Update(account);
                    context.SaveChanges();
                }
            }
        }

        private void AddNewAccount(Tuple<Account, IEnumerable<Like>> data)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = services.GetRequiredService<AccountContext>())
                {
                    context.Accounts.Add(data.Item1);
                    context.Likes.AddRange(data.Item2);
                    context.SaveChanges();
                }
            }
        }

        private void NewLikes(IEnumerable<Like> data)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = services.GetRequiredService<AccountContext>())
                {
                    context.Likes.AddRange(data);
                    context.SaveChanges();
                }
            }
        }
    }
}