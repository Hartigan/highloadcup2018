using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Threading.Tasks;

namespace AspNetCoreWebApi.Processing
{
    class DataLoader
    {
        private readonly IServiceProvider _services;
        private readonly CityStorage _cityStorage;
        private readonly InterestStorage _interestStorage;
        private readonly CountryStorage _countryStorage;

        public DataLoader(
            IServiceProvider services,
            AccountContext context,
            CityStorage cityStorage,
            InterestStorage interestStorage,
            CountryStorage countryStorage)
        {
            _services = services;
            _cityStorage = cityStorage;
            _interestStorage = interestStorage;
            _countryStorage = countryStorage;

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        public async Task Run(string path)
        {
            List<Task> tasks = new List<Task>();
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var accountDtos = ParseEntry(entry);
                        tasks.Add(SaveEntry(accountDtos));
                    }
                }
            }
            await Task.WhenAll(tasks);
            GC.Collect();
        }

        private IReadOnlyList<Account> ParseEntry(ZipArchiveEntry entry)
        {
            using (TextReader textReader = new StreamReader(entry.Open()))
            {
                var obj = JObject.Parse(textReader.ReadToEnd());
                var accountsArray = obj["accounts"];
                List<Account> accounts = new List<Account>(10000);
                foreach (var accountObj in accountsArray.Children())
                {
                    accounts.Add(ParseAccountObj(accountObj));
                }

                return accounts;
            }
        }

        private async Task SaveEntry(IReadOnlyList<Account> accounts)
        {
            using (var scope = _services.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = _services.GetRequiredService<AccountContext>())
                {
                    await context.Accounts.AddRangeAsync(accounts);
                    await context.SaveChangesAsync();
                }
            }
        }

        private Account ParseAccountObj(JToken accountObj)
        {
            Account result = new Account();

            AccountDto dto = JsonConvert.DeserializeObject<AccountDto>(accountObj.ToString());

            result.Id = dto.Id;
            result.Email = dto.Email;
            result.FirstName = dto.FirstName;
            result.LastName = dto.Surname;
            result.Phone = dto.Phone;
            result.Birth = DateTimeOffset.FromUnixTimeSeconds(dto.Birth);
            result.Joined = DateTimeOffset.FromUnixTimeSeconds(dto.Joined);
            switch (dto.Status)
            {
                case "свободны":
                    result.Status = Status.Free;
                    break;
                case "заняты":
                    result.Status = Status.Reserved;
                    break;
                case "всё сложно":
                    result.Status = Status.Complicated;
                    break;
            }

            if (dto.Country != null)
            {
                result.CountryId = _countryStorage.Get(dto.Country);
            }

            if (dto.City != null)
            {
                result.CityId = _cityStorage.Get(dto.City);
            }

            if (dto.Interests != null)
            {
                foreach (var interest in dto.Interests)
                {
                    result.Interests.Add(new Interest() { StringId = _interestStorage.Get(interest) });
                }
            }

            if (dto.Likes != null)
            {
                result.Likes = dto.Likes.Select(x => new Like() { LikerId = x.Id, Timestamp = DateTimeOffset.FromUnixTimeSeconds(x.Timestamp) }).ToList();
            }

            if (dto.Premium != null)
            {
                result.PremiumStart = DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Start);
                result.PremiumEnd = DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Finish);
            }
            return result;
        }
    }
}