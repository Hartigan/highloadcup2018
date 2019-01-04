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

namespace AspNetCoreWebApi.Processing
{
    public class EditAccountProcessor
    {
        private readonly MainStorage _storage;
        private Subject<AccountDto> _dataReceived = new Subject<AccountDto>();

        public EditAccountProcessor(
            MainStorage mainStorage)
        {
            _storage = mainStorage;
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
                _storage.EmailHashes.RemoveById(id);
                _storage.EmailHashes.Add(dto.Email, id);
            }

            if (dto.Phone != null)
            {
                _storage.PhoneHashes.RemoveById(id);
                _storage.PhoneHashes.Add(dto.Phone, id);
            }

        }

        private bool Validate(AccountDto dto)
        {
            if (dto.Email != null && _storage.EmailHashes.ContainsString(dto.Email))
            {
                return false;
            }

            if (dto.Phone != null && _storage.PhoneHashes.ContainsString(dto.Phone))
            {
                return false;
            }

            return true;
        }
    }
}