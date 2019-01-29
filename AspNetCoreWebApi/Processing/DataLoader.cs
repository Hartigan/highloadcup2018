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
using System.Threading;
using System.Reactive;

namespace AspNetCoreWebApi.Processing
{
    public class DataLoader
    {
        private readonly MainContext _context;
        private readonly MainPool _pool;

        private Subject<AccountDto> _accountLoaded = new Subject<AccountDto>();
        private Subject<Unit> _gc = new Subject<Unit>();
        private Subject<Like> _like = new Subject<Like>(); 

        public DataLoader(
            MainContext context,
            MainPool pool)
        {
            _pool = pool;
            _context = context;
        }

        public IObservable<Unit> CallGc => _gc;

        public void Config(string path)
        {
            using(StreamReader reader = new StreamReader(path))
            {
                DataConfig.NowSeconds = int.Parse(reader.ReadLine());
                DataConfig.Now = new UnixTime(DataConfig.NowSeconds);
            }
        }

        public IObservable<AccountDto> AccountLoaded => _accountLoaded;
        public IObservable<Like> LikeLoaded => _like;

        public void Run(string path)
        {
            Console.WriteLine($"Import started {DateTime.Now}");
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                int fileCount = 0;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseEntry(entry);
                    }
                    fileCount++;

                    if (GC.GetTotalMemory(false) > 1620000000)
                    {
                        Console.WriteLine($"Heap total bytes used: {GC.GetTotalMemory(true)}");
                        _gc.OnNext(Unit.Default);
                    } else if (fileCount % 10 == 0)
                    {
                        _gc.OnNext(Unit.Default);
                    }
                }
                _like.OnCompleted();
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

                    if (dto.Likes != null && dto.Likes.Count > 0)
                    {
                        for(int i = 0; i < dto.Likes.Count; i++)
                        {
                            _like.OnNext(
                                new Like(
                                    dto.Likes[i].Id,
                                    dto.Id.Value,
                                    new UnixTime(
                                        dto.Likes[i].Timestamp
                                    )
                                )
                            );
                        }
                        dto.Likes.Clear();
                    }
                    _accountLoaded.OnNext(dto);
                }
            }
            Console.WriteLine(entry.Name);
        }
    }
}