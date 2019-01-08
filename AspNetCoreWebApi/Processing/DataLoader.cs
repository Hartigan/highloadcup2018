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

        private Subject<IEnumerable<AccountDto>> _accountLoaded = new Subject<IEnumerable<AccountDto>>();

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

        public IObservable<IEnumerable<AccountDto>> AccountLoaded => _accountLoaded;

        public void Run(string path)
        {
            List<AccountDto> dtos = new List<AccountDto>(DataConfig.MaxId);
            DefaultObjectPool<AccountDto> pool = new DefaultObjectPool<AccountDto>(new AccountDtoPolicy());

            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseEntry(entry, dtos, pool);
                        _accountLoaded.OnNext(dtos);
                        foreach (var dto in dtos)
                        {
                            pool.Return(dto);
                        }
                        dtos.Clear();
                    }
                }
            }
            _context.Compress();
            Console.WriteLine("Import finished");
        }

        private void ParseEntry(ZipArchiveEntry entry, List<AccountDto> dtos, ObjectPool<AccountDto> pool)
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
                    AccountDto dto = pool.Get();
                    ser.Populate(jsonReader, dto);
                    dtos.Add(dto);
                }
            }
        }
    }
}