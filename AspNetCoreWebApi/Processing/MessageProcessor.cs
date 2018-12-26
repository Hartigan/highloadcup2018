using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
        private readonly IDisposable _filterProcessorSubscription;
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
            NewLikesProcessor newLikesProcessor,
            FilterProcessor filterProcessor)
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

            _filterProcessorSubscription = filterProcessor
                .DataRequest
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(Filter);
        }

        private void Filter(Tuple<TaskCompletionSource<IReadOnlyList<Account>>, Func<Query, Query>> data)
        {
            using (var scope1 = _serviceProvider.CreateScope())
            using (var scope2 = _serviceProvider.CreateScope())
            using (var scope3 = _serviceProvider.CreateScope())
            using (var context1 = scope1.ServiceProvider.GetRequiredService<AccountContext>())
            using (var context2 = scope2.ServiceProvider.GetRequiredService<AccountContext>())
            using (var context3 = scope3.ServiceProvider.GetRequiredService<AccountContext>())
            {
                data.Item1.SetResult(
                    data.Item2(
                        new Query(context1.Accounts, context2.Likes, context3.Interests)).Accounts.ToList());
            }
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
                    if (dto.Phone != null) 
                    {
                        account.Phone = dto.Phone;
                        account.Code = AccountParser.ExtractCode(dto.Phone);
                    }
                    if (dto.Birth != null) account.Birth = DateTimeOffset.FromUnixTimeSeconds(dto.Birth.Value);
                    if (dto.Country != null) account.CountryId = _countryStorage.Get(dto.Country);
                    if (dto.City != null) account.CityId = _cityStorage.Get(dto.City);
                    if (dto.Joined != null) account.Joined = DateTimeOffset.FromUnixTimeSeconds(dto.Joined.Value);
                    if (dto.Interests != null)
                    {
                        context.RemoveRange(context.Interests.Where(x => x.AccountId == account.Id));
                        context.AddRange(dto.Interests.Select(x => new Interest() { AccountId = account.Id, StringId = _interestStorage.Get(x) }));
                    } 
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

        private void AddNewAccount(ParserResult data)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = services.GetRequiredService<AccountContext>())
                {
                    context.Accounts.Add(data.Account);
                    context.Likes.AddRange(data.Likes);
                    context.Interests.AddRange(data.Interests);
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