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
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Processing
{
    public class NewAccountProcessor
    {
        private readonly MainStorage _storage;
        private Subject<AccountDto> _dataReceived = new Subject<AccountDto>();

        public NewAccountProcessor(
            MainStorage mainStorage)
        {
            _storage = mainStorage;
        }

        public IObservable<AccountDto> DataReceived => _dataReceived;

        public bool Process(Stream body)
        {
            AccountDto dto = null;
            try
            {
                using (StreamReader streamReader = new StreamReader(body))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    dto = (AccountDto)serializer.Deserialize(jsonTextReader, typeof(AccountDto));
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            if (!Validate(dto))
            {
                return false;
            }

            _dataReceived.OnNext(dto);
            return true;
        }

        private bool Validate(AccountDto dto)
        {
            if (!dto.Id.HasValue || _storage.Ids.Contains(dto.Id.Value))
            {
                return false;
            }

            if (!dto.Birth.HasValue)
            {
                return false;
            }

            if (!dto.Joined.HasValue)
            {
                return false;
            }

            if (_storage.EmailHashes.ContainsString(dto.Email))
            {
                return false;
            }

            if (dto.Phone != null && _storage.PhoneHashes.ContainsString(dto.Phone))
            {
                return false;
            }

            if (dto.Likes != null)
            {
                foreach (var like in dto.Likes)
                {
                    if (!_storage.Ids.Contains(like.Id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}