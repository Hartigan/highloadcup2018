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
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing
{
    public class DataLoader
    {
        private readonly MainContext _context;

        private Subject<AccountDto> _accountLoaded = new Subject<AccountDto>();

        public DataLoader(
            MainContext context)
        {
            _context = context;
        }

        public void Config(string path)
        {
            using(StreamReader reader = new StreamReader(path))
            {
                DataConfig.NowSeconds = int.Parse(reader.ReadLine());
                DataConfig.Now = DateTimeOffset.FromUnixTimeSeconds(DataConfig.NowSeconds);
            }
        }

        public IObservable<AccountDto> AccountLoaded => _accountLoaded;

        public void Run(string path)
        {
            List<Task> tasks = new List<Task>();
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseEntry(entry);
                    }
                }
            }
            Console.WriteLine("Import finished");
            GC.Collect();
        }

        private void ParseEntry(ZipArchiveEntry entry)
        {
            using (TextReader textReader = new StreamReader(entry.Open()))
            {
                var obj = JObject.Parse(textReader.ReadToEnd());
                var accountsArray = obj["accounts"];
                foreach (var accountObj in accountsArray.Children())
                {
                    _accountLoaded.OnNext(ParseAccountObj(accountObj));
                }
            }
        }

        private AccountDto ParseAccountObj(JToken accountObj)
        {
            return JsonConvert.DeserializeObject<AccountDto>(accountObj.ToString());
        }
    }
}