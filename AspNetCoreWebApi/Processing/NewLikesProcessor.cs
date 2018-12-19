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
        private readonly IdStorage _idStorage;
        private Subject<IEnumerable<Like>> _dataReceived = new Subject<IEnumerable<Like>>();

        public NewLikesProcessor(
            IdStorage idStorage)
        {
            _idStorage = idStorage;
        }

        public IObservable<IEnumerable<Like>> DataReceived => _dataReceived;

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

            _dataReceived.OnNext(dto.Likes.Select(
                x => new Like() 
                {
                    LikeeId = x.LikeeId,
                    LikerId = x.LikerId,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(x.Timestamp)
                }));
            return true;
        }

        private bool Validate(SingleLikeDto dto)
        {
            if (!_idStorage.Contains(dto.LikeeId))
            {
                return false;
            }

            if (!_idStorage.Contains(dto.LikerId))
            {
                return false;
            }

            return true;
        }
    }
}