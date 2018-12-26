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
        private readonly IdStorage _idStorage;
        private readonly EmailHashStorage _emailHashStorage;
        private readonly PhoneHashStorage _phoneHashStorage;
        private readonly AccountParser _accountParser;
        private Subject<ParserResult> _dataReceived = new Subject<ParserResult>();

        public NewAccountProcessor(
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

        public IObservable<ParserResult> DataReceived => _dataReceived;

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
            if (!dto.Id.HasValue || _idStorage.Contains(dto.Id.Value))
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

            if (_emailHashStorage.ContainsString(dto.Email))
            {
                return false;
            }

            if (dto.Phone != null && _phoneHashStorage.ContainsString(dto.Phone))
            {
                return false;
            }

            if (dto.Likes != null)
            {
                foreach (var like in dto.Likes)
                {
                    if (!_idStorage.Contains(like.Id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}