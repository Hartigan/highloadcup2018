using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.StringPools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Threading.Tasks;
using AspNetCoreWebApi.Processing.Parsers;

namespace AspNetCoreWebApi.Processing
{
    public class EditAccountProcessor
    {
        private readonly MainStorage _storage;
        private readonly Validator _validator;
        private Subject<AccountDto> _dataReceived = new Subject<AccountDto>();

        public EditAccountProcessor(
            MainStorage mainStorage,
            Validator validator)
        {
            _storage = mainStorage;
            _validator = validator;
        }

        public IObservable<AccountDto> DataReceived => _dataReceived;

        public bool Process(Stream body, int id)
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
            catch (Exception)
            {
                return false;
            }

            if (!Validate(dto))
            {
                return false;
            }

            UpdateHashes(dto, id);

            dto.Id = id;

            _dataReceived.OnNext(dto);
            return true;
        }

        private void UpdateHashes(AccountDto dto, int id)
        {
            if (dto.Email != null)
            {
                _storage.EmailHashes.ReplaceById(dto.Email, id);
            }

            if (dto.Phone != null)
            {
                _storage.PhoneHashes.ReplaceById(dto.Phone, id);
            }

        }

        private bool Validate(AccountDto dto)
        {
            if (dto.Email != null)
            {
                if (!_validator.Email(dto.Email) || _storage.EmailHashes.ContainsString(dto.Email))
                {
                    return false;
                }
            }

            if (dto.Phone != null)
            {
                if (!_validator.Phone(dto.Phone) || _storage.PhoneHashes.ContainsString(dto.Phone))
                {
                    return false;
                }
            }

            if (dto.Sex != null && !_validator.Sex(dto.Sex))
            {
                return false;
            }

            if (dto.Birth != null && !_validator.Birth(dto.Birth.Value))
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

            if (dto.Country != null && !_validator.Country(dto.Country))
            {
                return false;
            }

            if (dto.City != null && !_validator.City(dto.City))
            {
                return false;
            }

            if (dto.Joined != null && !_validator.Joined(dto.Joined.Value))
            {
                return false;
            }

            if (dto.Status != null && !_validator.Status(dto.Status))
            {
                return false;
            }

            if (dto.Interests != null && dto.Interests.Any(x => !_validator.Interest(x)))
            {
                return false;
            }

            if (dto.Premium != null && !_validator.Premium(dto.Premium.Start, dto.Premium.Finish))
            {
                return false;
            }

            return true;
        }
    }
}