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
    public class EditAccountProcessor
    {
        private readonly IdStorage _idStorage;
        private readonly EmailHashStorage _emailHashStorage;
        private readonly PhoneHashStorage _phoneHashStorage;
        private readonly AccountParser _accountParser;
        private Subject<Tuple<int, AccountDto>> _dataReceived = new Subject<Tuple<int, AccountDto>>();

        public EditAccountProcessor(
            IdStorage idStorage,
            EmailHashStorage emailHashStorage,
            PhoneHashStorage phoneHashStorage,
            AccountParser accountParser)
        {
            _idStorage = idStorage;
            _emailHashStorage = emailHashStorage;
            _phoneHashStorage = phoneHashStorage;
            _accountParser = accountParser;
        }

        public IObservable<Tuple<int, AccountDto>> DataReceived => _dataReceived;

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
            catch (Exception ex)
            {
                return false;
            }

            if (!Validate(dto))
            {
                return false;
            }

            UpdateHashes(dto, id);

            _dataReceived.OnNext(new Tuple<int, AccountDto>(id, dto));
            return true;
        }

        private void UpdateHashes(AccountDto dto, int id)
        {
            if (dto.Email != null)
            {
                _emailHashStorage.RemoveById(id);
                _emailHashStorage.Add(dto.Email, id);
            }

            if (dto.Phone != null)
            {
                _phoneHashStorage.RemoveById(id);
                _phoneHashStorage.Add(dto.Phone, id);
            }

        }

        private bool Validate(AccountDto dto)
        {
            if (dto.Email != null && _emailHashStorage.ContainsString(dto.Email))
            {
                return false;
            }

            if (dto.Phone != null && _phoneHashStorage.ContainsString(dto.Phone))
            {
                return false;
            }

            return true;
        }
    }
}