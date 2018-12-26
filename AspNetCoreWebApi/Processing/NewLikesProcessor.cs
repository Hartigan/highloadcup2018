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
    public class NewLikesProcessor
    {
        private readonly MainStorage _storage;
        private Subject<IReadOnlyList<SingleLikeDto>> _dataReceived = new Subject<IReadOnlyList<SingleLikeDto>>();

        public NewLikesProcessor(
            MainStorage mainStorage)
        {
            _storage = mainStorage;
        }

        public IObservable<IReadOnlyList<SingleLikeDto>> DataReceived => _dataReceived;

        public bool Process(Stream body)
        {
            LikesDto dto = new LikesDto();
            try
            {
                using (StreamReader streamReader = new StreamReader(body))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    dto = (LikesDto)serializer.Deserialize(jsonTextReader, typeof(LikesDto));
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            if (dto.Likes.Any(x => !Validate(x)))
            {
                return false;
            }

            _dataReceived.OnNext(dto.Likes);
            return true;
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