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
using AspNetCoreWebApi.Storage.Contexts;
using Microsoft.Extensions.ObjectPool;
using AspNetCoreWebApi.Processing.Pooling;

namespace AspNetCoreWebApi.Processing
{
    public class DataLoader
    {
        private readonly MainContext _context;
        private readonly MainPool _pool;

        private Subject<AccountDto> _accountLoaded = new Subject<AccountDto>();

        public DataLoader(
            MainContext context,
            MainPool pool)
        {
            _pool = pool;
            _context = context;
        }

        public void Config(string path)
        {
            using(StreamReader reader = new StreamReader(path))
            {
                DataConfig.NowSeconds = int.Parse(reader.ReadLine());
                DataConfig.Now = new UnixTime(DataConfig.NowSeconds);
            }
        }

        public IObservable<AccountDto> AccountLoaded => _accountLoaded;

        public void Run(string path)
        {
            Console.WriteLine($"Import started {DateTime.Now}");
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseEntry(entry);
                    }
                }
                _accountLoaded.OnCompleted();
            }
        }

        private void ParseEntry(ZipArchiveEntry entry)
        {
            JsonSerializer ser = JsonSerializer.CreateDefault();
            using (TextReader textReader = new StreamReader(entry.Open()))
            using (JsonTextReader jsonReader = new JsonTextReader(textReader))
            {
                jsonReader.Read();
                jsonReader.Read();
                jsonReader.Read();
                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    AccountDto dto = _pool.AccountDto.Get();
                    ser.Populate(jsonReader, dto);
                    _accountLoaded.OnNext(dto);
                }
            }
        }
    }
}