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
        private readonly AccountParser _accountParser;

        public DataLoader(
            IServiceProvider services,
            AccountContext context,
            AccountParser accountParser)
        {
            _services = services;
            _accountParser = accountParser;
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

                    if (entry.FullName == "options.txt")
                    {
                        ParseConfig(entry);
                    }
                }
            }
            await Task.WhenAll(tasks);
            Console.WriteLine("Import finished");
            GC.Collect();
        }

        private static void ParseConfig(ZipArchiveEntry entry)
        {
            using (TextReader optionsReader = new StreamReader(entry.Open()))
            {
                DataConfig.NowSeconds = int.Parse(optionsReader.ReadLine());
                DataConfig.Now = DateTimeOffset.FromUnixTimeSeconds(DataConfig.NowSeconds);
            }
        }

        private IReadOnlyList<ParserResult> ParseEntry(ZipArchiveEntry entry)
        {
            using (TextReader textReader = new StreamReader(entry.Open()))
            {
                var obj = JObject.Parse(textReader.ReadToEnd());
                var accountsArray = obj["accounts"];
                List<ParserResult> data = new List<ParserResult>(10000);
                foreach (var accountObj in accountsArray.Children())
                {
                    data.Add(ParseAccountObj(accountObj));
                }

                return data;
            }
        }

        private async Task SaveEntry(IReadOnlyList<ParserResult> data)
        {
            using (var scope = _services.CreateScope())
            {
                var services = scope.ServiceProvider;
                using (var context = _services.GetRequiredService<AccountContext>())
                {
                    await context.Accounts.AddRangeAsync(data.Select(x=>x.Account));
                    await context.Likes.AddRangeAsync(data.SelectMany(x => x.Likes));
                    await context.Interests.AddRangeAsync(data.SelectMany(x => x.Interests));
                    await context.SaveChangesAsync();
                }
            }
        }

        private ParserResult ParseAccountObj(JToken accountObj)
        {
            AccountDto dto = JsonConvert.DeserializeObject<AccountDto>(accountObj.ToString());
            return _accountParser.Parse(dto);
        }
    }
}