using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AspNetCoreWebApi.Processing
{
    public class FilterProcessor
    {

        private readonly CountryStorage _countryStorage;
        private readonly CityStorage _cityStorage;
        private readonly InterestStorage _interestStorage;
        private readonly IdStorage _idStorage;

        private Subject<Tuple<TaskCompletionSource<IReadOnlyList<Account>>, Func<Query, Query>>> _dataRequest = new Subject<Tuple<TaskCompletionSource<IReadOnlyList<Account>>, Func<Query, Query>>>();

        public IObservable<Tuple<TaskCompletionSource<IReadOnlyList<Account>>, Func<Query, Query>>> DataRequest => _dataRequest;

        public FilterProcessor(
            CountryStorage countryStorage,
            CityStorage cityStorage,
            InterestStorage interestStorage,
            IdStorage idStorage
        )
        {
            _countryStorage = countryStorage;
            _cityStorage = cityStorage;
            _interestStorage = interestStorage;
            _idStorage = idStorage;
        }

        public async Task<bool> Process(HttpResponse httpResponse, IQueryCollection query)
        {
            Func<Query, Query> request = x => x; 
            HashSet<Field> fields = new HashSet<Field>();

            int limit = 0;
            foreach (var filter in query)
            {
                switch(filter.Key)
                {
                    case "query_id":
                        break;

                    case "limit":
                        if (!int.TryParse(filter.Value,  out limit))
                        {
                            return false;
                        }
                        break;

                    case "sex_eq":
                        request = SexEq(request, filter.Value);
                        fields.Add(Field.Sex);
                        break;

                    case "email_domain":
                        request = EmailDomain(request, filter.Value);
                        break;

                    case "email_lt":
                        request = EmailLt(request, filter.Value);
                        break;

                    case "email_gt":
                        request = EmailGt(request, filter.Value);
                        break;

                    case "status_eq":
                        request = StatusEq(request, filter.Value);
                        fields.Add(Field.Status);
                        break;

                    case "status_neq":
                        request = StatusNeq(request, filter.Value);
                        fields.Add(Field.Status);
                        break;

                    case "fname_eq":
                        request = FnameEq(request, filter.Value);
                        fields.Add(Field.FName);
                        break;

                    case "fname_any":
                        request = FnameAny(request, filter.Value);
                        fields.Add(Field.FName);
                        break;

                    case "fname_null":
                        request = FnameNull(request, filter.Value);
                        fields.Add(Field.FName);
                        break;

                    case "sname_eq":
                        request = SnameEq(request, filter.Value);
                        fields.Add(Field.SName);
                        break;

                    case "sname_starts":
                        request = SnameStarts(request, filter.Value);
                        fields.Add(Field.SName);
                        break;

                    case "sname_null":
                        request = SnameNull(request, filter.Value);
                        fields.Add(Field.SName);
                        break;

                    case "phone_code":
                        request = PhoneCode(request, filter.Value);
                        fields.Add(Field.Phone);
                        break;

                    case "phone_null":
                        request = PhoneNull(request, filter.Value);
                        fields.Add(Field.Phone);
                        break;

                    case "country_eq":
                        request = CountryEq(request, filter.Value);
                        fields.Add(Field.Country);
                        break;

                    case "country_null":
                        request = CountryNull(request, filter.Value);
                        fields.Add(Field.Country);
                        break;

                    case "city_eq":
                        request = CityEq(request, filter.Value);
                        fields.Add(Field.City);
                        break;

                    case "city_any":
                        request = CityAny(request, filter.Value);
                        fields.Add(Field.City);
                        break;

                    case "city_null":
                        request = CityNull(request, filter.Value);
                        fields.Add(Field.City);
                        break;

                    case "birth_lt":
                        request = BirthLt(request, filter.Value);
                        fields.Add(Field.Birth);
                        break;

                    case "birth_gt":
                        request = BirthGt(request, filter.Value);
                        fields.Add(Field.Birth);
                        break;

                    case "birth_year":
                        request = BirthYear(request, filter.Value);
                        fields.Add(Field.Birth);
                        break;

                    case "interests_contains":
                        request = InterestsContains(request, filter.Value);
                        break;

                    case "interests_any":
                        request = InterestsAny(request, filter.Value);
                        break;

                    case "likes_contains":
                        request = LikesContains(request, filter.Value);
                        break;

                    case "premium_now":
                        request = PremiumNow(request, filter.Value);
                        fields.Add(Field.Premium);
                        break;

                    case "premium_null":
                        request = PremiumNull(request, filter.Value);
                        fields.Add(Field.Premium);
                        break;

                    default:
                        return false;
                }
            }

            request = LimitAndSort(request, limit);

            TaskCompletionSource<IReadOnlyList<Account>> tcs = new TaskCompletionSource<IReadOnlyList<Account>>();

            _dataRequest.OnNext(new Tuple<TaskCompletionSource<IReadOnlyList<Account>>, Func<Query, Query>>(tcs, request));

            var result = await tcs.Task;

            var printer = new AccountPrinter(fields.ToArray(), _countryStorage, _cityStorage);

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";
            using(var sw = new StreamWriter(httpResponse.Body))
            {
                printer.WriteFilterResponse(result, sw);
            }

            return true;
        }

        private Func<Query, Query> LimitAndSort(Func<Query, Query> request, int limit)
        {
            return x => x.Create(request(x).Accounts.OrderBy(account => account.Id).Take(limit));
        }

        private Func<Query, Query> PremiumNull(Func<Query, Query> request, StringValues value)
        {
            if (value == "1")
            {
                return x => x.Create(request(x).Accounts.Where(account => !account.PremiumStart.HasValue));
            }
            else if (value == "0")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.PremiumStart.HasValue));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> PremiumNow(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(
                request(x)
                    .Accounts
                    .Where(account => account.PremiumStart < DataConfig.Now && account.PremiumEnd > DataConfig.Now));
        }

        private Func<Query, Query> LikesContains(Func<Query, Query> request, StringValues value)
        {
            HashSet<int> ids = new HashSet<int>(value.Count);
            foreach(var strId in value)
            {
                int id;
                if (int.TryParse(strId, out id))
                {
                    if (!_idStorage.Contains(id))
                    {
                        return Empty();
                    }
                    else
                    {
                        ids.Add(id);
                    }
                }
            }

            return x => x.Create(request(x).Accounts
                                .Join(
                                    x.Likes
                                        .GroupBy(like => like.LikerId, like => like.LikeeId)
                                        .Where(ids.IsSubsetOf)
                                        .Select(likes => likes.Key),
                                    account => account.Id,
                                    id => id,
                                    (account, id) => account));
        }

        private Func<Query, Query> InterestsAny(Func<Query, Query> request, StringValues value)
        {
            HashSet<int> ids = new HashSet<int>(value.Count);
            foreach(var interest in value)
            {
                int id;
                if (!_interestStorage.TryGet(interest, out id))
                {
                    return Empty();
                }
                else
                {
                    ids.Add(id);
                }
            }

            return x => x.Create(request(x).Accounts
                                .Join(
                                    x.Interests
                                        .GroupBy(interest => interest.AccountId, interest => interest.StringId)
                                        .Where(interests => ids.Intersect(interests).Any())
                                        .Select(likes => likes.Key),
                                    account => account.Id,
                                    id => id,
                                    (account, id) => account));
        }

        private Func<Query, Query> InterestsContains(Func<Query, Query> request, StringValues value)
        {
            HashSet<int> ids = new HashSet<int>(value.Count);
            foreach(var interest in value)
            {
                int id;
                if (!_interestStorage.TryGet(interest, out id))
                {
                    return Empty();
                }
                else
                {
                    ids.Add(id);
                }
            }

            return x => x.Create(request(x).Accounts
                                .Join(
                                    x.Interests
                                        .GroupBy(interest => interest.AccountId, interest => interest.StringId)
                                        .Where(ids.IsSubsetOf)
                                        .Select(likes => likes.Key),
                                    account => account.Id,
                                    id => id,
                                    (account, id) => account));
        }

        private Func<Query, Query> BirthYear(Func<Query, Query> request, StringValues value)
        {
            int year;
            if (int.TryParse(value, out year))
            {
                return x => x.Create(request(x).Accounts.Where(account => account.Birth.Year == year));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> BirthGt(Func<Query, Query> request, StringValues value)
        {
            int ts;
            if (int.TryParse(value, out ts))
            {
                DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(ts);
                return x => x.Create(request(x).Accounts.Where(account => account.Birth > dto));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> BirthLt(Func<Query, Query> request, StringValues value)
        {
            int ts;
            if (int.TryParse(value, out ts))
            {
                DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(ts);
                return x => x.Create(request(x).Accounts.Where(account => account.Birth < dto));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> CityNull(Func<Query, Query> request, StringValues value)
        {
            if (value == "1")
            {
                return x => x.Create(request(x).Accounts.Where(account => !account.CityId.HasValue));
            }
            else if (value == "0")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.CityId.HasValue));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> CityAny(Func<Query, Query> request, StringValues value)
        {
            HashSet<int> cityIds = new HashSet<int>(value.Count());
            foreach (var city in value)
            {
                int cityId;
                if (_cityStorage.TryGet(city, out cityId))
                {
                    cityIds.Add(cityId);
                }
            }

            if (cityIds.Count > 0)
            {
                return x => x.Create(request(x).Accounts.Where(account => account.CityId.HasValue && cityIds.Contains(account.CountryId.Value)));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> CityEq(Func<Query, Query> request, StringValues value)
        {
            int cityId;
            if (_cityStorage.TryGet(value, out cityId))
            {
                return x => x.Create(request(x).Accounts.Where(account => account.CityId == cityId));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> CountryNull(Func<Query, Query> request, StringValues value)
        {
            if (value == "1")
            {
                return x => x.Create(request(x).Accounts.Where(account => !account.CountryId.HasValue));
            }
            else if (value == "0")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.CountryId.HasValue));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> CountryEq(Func<Query, Query> request, StringValues value)
        {
            int countryId;
            if (_countryStorage.TryGet(value, out countryId))
            {
                return x => x.Create(request(x).Accounts.Where(account => account.CountryId == countryId));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> PhoneNull(Func<Query, Query> request, StringValues value)
        {
            if (value == "1")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.Phone == null));
            }
            else if (value == "0")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.Phone != null));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> PhoneCode(Func<Query, Query> request, StringValues value)
        {
            int code;
            if (int.TryParse(value, out code))
            {
                return x => x.Create(x.Accounts.Where(account => account.Code == code));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> SnameNull(Func<Query, Query> request, StringValues value)
        {
            if (value == "1")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.LastName == null));
            }
            else if (value == "0")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.LastName != null));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> SnameStarts(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => account.LastName != null && account.LastName.StartsWith(value)));
        }

        private Func<Query, Query> SnameEq(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => string.Equals(account.LastName, value)));
        }

        private Func<Query, Query> FnameNull(Func<Query, Query> request, StringValues value)
        {
            if (value == "1")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.FirstName == null));
            }
            else if (value == "0")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.FirstName != null));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> FnameAny(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => value.Contains(account.FirstName)));
        }

        private Func<Query, Query> FnameEq(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => string.Equals(account.FirstName, value)));
        }

        private Func<Query, Query> StatusNeq(Func<Query, Query> request, StringValues value)
        {
            Status status;
            if (StatusHelper.TryParse(value, out status))
            {
                return x => x.Create(request(x).Accounts.Where(account => account.Status != status));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> StatusEq(Func<Query, Query> request, StringValues value)
        {
            Status status;
            if (StatusHelper.TryParse(value, out status))
            {
                return x => x.Create(request(x).Accounts.Where(account => account.Status == status));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> EmailGt(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => string.Compare(account.Email, value) > 0));
        }

        private Func<Query, Query> EmailLt(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => string.Compare(account.Email, value) < 0));
        }

        private Func<Query, Query> EmailDomain(Func<Query, Query> request, StringValues value)
        {
            return x => x.Create(request(x).Accounts.Where(account => account.Email.EndsWith("@" + value)));
        }

        private Func<Query, Query> SexEq(Func<Query, Query> request, StringValues value)
        {
            if (value == "m")
            {
                return x => x.Create(request(x).Accounts.Where(account => account.Sex));
            }
            else if (value == "f")
            {
                return x => x.Create(request(x).Accounts.Where(account => !account.Sex));
            }
            else
            {
                return Empty();
            }
        }

        private Func<Query, Query> Empty()
        {
            return x => x.Create(Enumerable.Empty<Account>().AsQueryable());
        }
    }
}