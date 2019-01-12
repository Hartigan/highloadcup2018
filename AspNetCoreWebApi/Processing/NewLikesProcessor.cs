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
using AspNetCoreWebApi.Processing.Pooling;

namespace AspNetCoreWebApi.Processing
{
    public class NewLikesProcessor
    {
        private readonly MainStorage _storage;
        private readonly Subject<List<SingleLikeDto>> _dataReceived = new Subject<List<SingleLikeDto>>();
        private readonly MainPool _pool;

        public NewLikesProcessor(
            MainStorage mainStorage,
            MainPool mainPool)
        {
            _storage = mainStorage;
            _pool = mainPool;
        }

        public IObservable<List<SingleLikeDto>> DataReceived => _dataReceived;

        public bool Process(Stream body)
        {
            var dtos = _pool.ListOfLikeDto.Get();

            try
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault();
                using (StreamReader streamReader = new StreamReader(body))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    jsonTextReader.Read();
                    jsonTextReader.Read();
                    jsonTextReader.Read();
                    while (jsonTextReader.Read() && jsonTextReader.TokenType != JsonToken.EndArray)
                    {
                        var dto = _pool.SingleLikeDto.Get();
                        serializer.Populate(jsonTextReader, dto);
                        dtos.Add(dto);
                    }
                }
            }
            catch(Exception)
            {
                Free(dtos);
                return false;
            }

            if (dtos.Any(x => !Validate(x)))
            {
                Free(dtos);
                return false;
            }

            _dataReceived.OnNext(dtos);
            return true;
        }

        private void Free(List<SingleLikeDto> dtos)
        {
            foreach (var dto in dtos)
            {
                _pool.SingleLikeDto.Return(dto);
            }
            _pool.ListOfLikeDto.Return(dtos);
        }

        private bool Validate(SingleLikeDto dto)
        {
            if (!_storage.Ids.Contains(dto.LikeeId))
            {
                return false;
            }

            if (!_storage.Ids.Contains(dto.LikerId))
            {
                return false;
            }

            return true;
        }
    }
}