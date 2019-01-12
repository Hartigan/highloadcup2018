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
using AspNetCoreWebApi.Processing.Parsers;
using AspNetCoreWebApi.Processing.Pooling;

namespace AspNetCoreWebApi.Processing
{
    public class NewAccountProcessor
    {
        private readonly Validator _validator;
        private readonly MainStorage _storage;
        private readonly Subject<AccountDto> _dataReceived = new Subject<AccountDto>();
        private readonly MainPool _pool;

        public NewAccountProcessor(
            Validator validator,
            MainPool mainPool,
            MainStorage mainStorage)
        {
            _validator = validator;
            _storage = mainStorage;
            _pool = mainPool;
        }

        public IObservable<AccountDto> DataReceived => _dataReceived;

        public bool Process(Stream body)
        {
            AccountDto dto = _pool.AccountDto.Get();;

            try
            {
                using (StreamReader streamReader = new StreamReader(body))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    JsonSerializer serializer = JsonSerializer.CreateDefault();
                    serializer.Populate(jsonTextReader, dto);
                }
            }
            catch(Exception)
            {
                _pool.AccountDto.Return(dto);
                return false;
            }
           

            if (!Validate(dto))
            {
                _pool.AccountDto.Return(dto);
                return false;
            }

            UpdateHashes(dto);  

            _dataReceived.OnNext(dto);
            return true;
        }

        private void UpdateHashes(AccountDto dto)
        {
            _storage.Ids.Add(dto.Id.Value);
            _storage.EmailHashes.Add(dto.Email, dto.Id.Value);

            if (dto.Phone != null)
            {
                _storage.PhoneHashes.Add(dto.Phone, dto.Id.Value);
            }

        }

        private bool Validate(AccountDto dto)
        {
            if (!dto.Id.HasValue || _storage.Ids.Contains(dto.Id.Value))
            {
                return false;
            }

            if (dto.Email == null || !_validator.Email(dto.Email))
            {
                return false;
            }

            if (dto.Surname != null && !_validator.Surname(dto.Surname))
            {
                return false;
            }

            if (dto.FirstName != null && !_validator.FirstName(dto.FirstName))
            {
                return false;
            }

            if (!dto.Birth.HasValue || !_validator.Birth(dto.Birth.Value))
            {
                return false;
            }

            if (dto.Country != null && !_validator.Country(dto.Country))
            {
                return false;
            }

            if (dto.City != null && !_validator.City(dto.City))
            {
                return false;
            }

            if (!dto.Joined.HasValue || !_validator.Joined(dto.Joined.Value))
            {
                return false;
            }

            if (dto.Status == null || !_validator.Status(dto.Status))
            {
                return false;
            }

            if (dto.Interests != null && dto.Interests.Any(x => !_validator.Interest(x)))
            {
                return false;
            }

            if (_storage.EmailHashes.ContainsString(dto.Email))
            {
                return false;
            }

            if (dto.Premium != null && !_validator.Premium(dto.Premium.Start, dto.Premium.Finish))
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