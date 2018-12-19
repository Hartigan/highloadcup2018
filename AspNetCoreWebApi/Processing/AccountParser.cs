using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Storage;

namespace AspNetCoreWebApi.Processing
{
    public class AccountParser
    {
        private readonly CityStorage _cityStorage;
        private readonly InterestStorage _interestStorage;
        private readonly CountryStorage _countryStorage;
        private readonly IdStorage _idStorage;
        private readonly EmailHashStorage _emailHashStorage;
        private readonly PhoneHashStorage _phoneHashStorage;

        public AccountParser(
            CityStorage cityStorage,
            InterestStorage interestStorage,
            CountryStorage countryStorage,
            IdStorage idStorage,
            EmailHashStorage emailHashStorage,
            PhoneHashStorage phoneHashStorage)
        {
            _cityStorage = cityStorage;
            _interestStorage = interestStorage;
            _countryStorage = countryStorage;
            _idStorage = idStorage;
            _emailHashStorage = emailHashStorage;
            _phoneHashStorage = phoneHashStorage;
        }

        public Tuple<Account, IEnumerable<Like>> Parse(AccountDto dto)
        {
            Account result = new Account();
            result.Id = dto.Id.Value;
            _idStorage.Add(result.Id);

            result.Email = dto.Email;
            _emailHashStorage.Add(result.Email, result.Id);

            result.Phone = dto.Phone;
            if (result.Phone != null)
            {
                _phoneHashStorage.Add(result.Phone, result.Id);
            }

            result.FirstName = dto.FirstName;
            result.LastName = dto.Surname;

            result.Birth = DateTimeOffset.FromUnixTimeSeconds(dto.Birth.Value);
            result.Joined = DateTimeOffset.FromUnixTimeSeconds(dto.Joined.Value);

            Status status = StatusHelper.Parse(dto.Status);
            result.Status = status;

            if (dto.Country != null)
            {
                result.CountryId = _countryStorage.Get(dto.Country);
            }

            if (dto.City != null)
            {
                result.CityId = _cityStorage.Get(dto.City);
            }

            if (dto.Interests != null)
            {
                foreach (var interest in dto.Interests)
                {
                    result.Interests.Add(new Interest() { StringId = _interestStorage.Get(interest) });
                }
            }

            IEnumerable<Like> likes = Enumerable.Empty<Like>();

            if (dto.Likes != null)
            {
                likes = dto.Likes.Select(x => new Like() 
                    { 
                        LikeeId = dto.Id.Value,
                        LikerId = x.Id, 
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(x.Timestamp)
                    }).ToList();
            }

            if (dto.Premium != null)
            {
                result.PremiumStart = DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Start);
                result.PremiumEnd = DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Finish);
            }

            return new Tuple<Account, IEnumerable<Like>>(result, likes);
        }
    }
}