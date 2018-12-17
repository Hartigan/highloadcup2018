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
    public class NewAccountProcessor
    {
        private readonly IServiceProvider _services;
        private readonly IdHashStorage _idHashStorage;
        private readonly EmailHashStorage _emailHashStorage;
        private readonly PhoneHashStorage _phoneHashStorage;
        private readonly AccountParser _accountParser;
        private Subject<Tuple<Account, IEnumerable<Like>>> _dataReceived = new Subject<Tuple<Account, IEnumerable<Like>>>();

        public NewAccountProcessor(
            IServiceProvider services,
            IdHashStorage idHashStorage,
            EmailHashStorage emailHashStorage,
            PhoneHashStorage phoneHashStorage,
            AccountParser accountParser)
        {
            _services = services;
            _idHashStorage = idHashStorage;
            _emailHashStorage = emailHashStorage;
            _phoneHashStorage = phoneHashStorage;
            _accountParser = accountParser;
        }

        public IObservable<Tuple<Account, IEnumerable<Like>>> DataReceived => _dataReceived;

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

            _dataReceived.OnNext(_accountParser.Parse(dto));
            return true;
        }

        private bool Validate(AccountDto dto)
        {
            if (_idHashStorage.Contains(dto.Id))
            {
                return false;
            }

            if (_emailHashStorage.Contains(dto.Email))
            {
                return false;
            }

            if (dto.Phone != null && _phoneHashStorage.Contains(dto.Phone))
            {
                return false;
            }

            if (dto.Likes != null)
            {
                foreach (var like in dto.Likes)
                {
                    if (!_idHashStorage.Contains(like.Id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}