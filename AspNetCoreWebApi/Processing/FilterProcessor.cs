using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Printers;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AspNetCoreWebApi.Processing
{
    public class FilterProcessor
    {

        private readonly MainStorage _storage;

        private readonly MainContext _context;

        private Subject<FilterRequest> _dataRequest = new Subject<FilterRequest>();

        public IObservable<FilterRequest> DataRequest => _dataRequest;

        public FilterProcessor(
            MainStorage mainStorage,
            MainContext mainContext
        )
        {
            _storage = mainStorage;
            _context = mainContext;
        }

        public async Task<bool> Process(HttpResponse httpResponse, IQueryCollection query)
        {
            FilterRequest request = new FilterRequest();
            HashSet<Field> fields = new HashSet<Field>();

            int limit = 0;
            foreach (var filter in query)
            {
                bool result = true;
                switch(filter.Key)
                {
                    case "query_id":
                        break;

                    case "limit":
                        if (!int.TryParse(filter.Value,  out limit))
                        {
                            return false;
                        }
                        else
                        {
                            if (limit <= 0)
                            {
                                return false;
                            }
                            request.Limit = limit;
                        }
                        break;

                    case "sex_eq":
                        result = SexEq(request, filter.Value);
                        fields.Add(Field.Sex);
                        break;

                    case "email_domain":
                        result = EmailDomain(request, filter.Value);
                        break;

                    case "email_lt":
                        result = EmailLt(request, filter.Value);
                        break;

                    case "email_gt":
                        result = EmailGt(request, filter.Value);
                        break;

                    case "status_eq":
                        result = StatusEq(request, filter.Value);
                        fields.Add(Field.Status);
                        break;

                    case "status_neq":
                        result = StatusNeq(request, filter.Value);
                        fields.Add(Field.Status);
                        break;

                    case "fname_eq":
                        result = FnameEq(request, filter.Value);
                        fields.Add(Field.FName);
                        break;

                    case "fname_any":
                        result = FnameAny(request, filter.Value.ToString().Split(','));
                        fields.Add(Field.FName);
                        break;

                    case "fname_null":
                        result = FnameNull(request, filter.Value);
                        fields.Add(Field.FName);
                        break;

                    case "sname_eq":
                        result = SnameEq(request, filter.Value);
                        fields.Add(Field.SName);
                        break;

                    case "sname_starts":
                        result = SnameStarts(request, filter.Value);
                        fields.Add(Field.SName);
                        break;

                    case "sname_null":
                        result = SnameNull(request, filter.Value);
                        fields.Add(Field.SName);
                        break;

                    case "phone_code":
                        result = PhoneCode(request, filter.Value);
                        fields.Add(Field.Phone);
                        break;

                    case "phone_null":
                        result = PhoneNull(request, filter.Value);
                        fields.Add(Field.Phone);
                        break;

                    case "country_eq":
                        result = CountryEq(request, filter.Value);
                        fields.Add(Field.Country);
                        break;

                    case "country_null":
                        result = CountryNull(request, filter.Value);
                        fields.Add(Field.Country);
                        break;

                    case "city_eq":
                        result = CityEq(request, filter.Value);
                        fields.Add(Field.City);
                        break;

                    case "city_any":
                        result = CityAny(request, filter.Value.ToString().Split(','));
                        fields.Add(Field.City);
                        break;

                    case "city_null":
                        result = CityNull(request, filter.Value);
                        fields.Add(Field.City);
                        break;

                    case "birth_lt":
                        result = BirthLt(request, filter.Value);
                        fields.Add(Field.Birth);
                        break;

                    case "birth_gt":
                        result = BirthGt(request, filter.Value);
                        fields.Add(Field.Birth);
                        break;

                    case "birth_year":
                        result = BirthYear(request, filter.Value);
                        fields.Add(Field.Birth);
                        break;

                    case "interests_contains":
                        result = InterestsContains(request, filter.Value.ToString().Split(','));
                        break;

                    case "interests_any":
                        result = InterestsAny(request, filter.Value.ToString().Split(','));
                        break;

                    case "likes_contains":
                        result = LikesContains(request, filter.Value.ToString().Split(','));
                        break;

                    case "premium_now":
                        result = PremiumNow(request, filter.Value);
                        fields.Add(Field.Premium);
                        break;

                    case "premium_null":
                        result = PremiumNull(request, filter.Value);
                        fields.Add(Field.Premium);
                        break;

                    default:
                        return false;
                }
                if (!result)
                {
                    return false;
                }
            }

            _dataRequest.OnNext(request);
            var response = await request.TaskComletionSource.Task;

            var printer = new AccountPrinter(fields.ToArray(), _storage, _context);

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";
            using(var sw = new StreamWriter(httpResponse.Body))
            {
                printer.Write(response, sw);
            }

            return true;
        }

        private bool PremiumNull(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Premium.IsActive = true;
            if (v == "1")
            {
                request.Premium.IsNull = true;
            }
            else if (v == "0")
            {
                request.Premium.IsNull = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool PremiumNow(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Premium.IsActive = true;
            if (v == "1")
            {
                request.Premium.Now = true;
            }
            else if (v == "0")
            {
                request.Premium.Now = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool LikesContains(FilterRequest request, StringValues value)
        {
            request.Likes.IsActive = true;
            List<int> ids = new List<int>(value.Count);
            foreach(var v in value)
            {
                int id;
                if (int.TryParse(v, out id))
                {
                    ids.Add(id);
                }
                else
                {
                    return false;
                }
            }
            request.Likes.Contains = ids;

            return true;
        }

        private bool InterestsAny(FilterRequest request, StringValues value)
        {
            request.Interests.IsActive = true;
            request.Interests.Any = value;
            return true;
        }

        private bool InterestsContains(FilterRequest request, StringValues value)
        {
            request.Interests.IsActive = true;
            request.Interests.Contains = value;
            return true;
        }

        private bool BirthYear(FilterRequest request, StringValues value)
        {
            request.Birth.IsActive = true;
            int year;
            if (int.TryParse(value, out year))
            {
                request.Birth.Year = year;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool BirthGt(FilterRequest request, StringValues value)
        {
            request.Birth.IsActive = true;
            int gt;
            if (int.TryParse(value, out gt))
            {
                var gtOffset = DateTimeOffset.FromUnixTimeSeconds(gt);
                if (request.Birth.Gt.HasValue)
                {
                    request.Birth.Gt = gtOffset > request.Birth.Gt.Value ? gtOffset : request.Birth.Gt;
                }
                else
                {
                    request.Birth.Gt = gtOffset;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool BirthLt(FilterRequest request, StringValues value)
        {
            request.Birth.IsActive = true;
            int lt;
            if (int.TryParse(value, out lt))
            {
                var ltOffset = DateTimeOffset.FromUnixTimeSeconds(lt);
                if (request.Birth.Lt.HasValue)
                {
                    request.Birth.Lt = ltOffset < request.Birth.Lt.Value ? ltOffset : request.Birth.Lt;
                }
                else
                {
                    request.Birth.Lt = ltOffset;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CityNull(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.City.IsActive = true;
            if (v == "1")
            {
                request.City.IsNull = true;
            }
            else if (v == "0")
            {
                request.City.IsNull = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool CityAny(FilterRequest request, StringValues value)
        {
            request.City.IsActive = true;
            request.City.Any = value;
            return true;
        }

        private bool CityEq(FilterRequest request, StringValues value)
        {
            request.City.IsActive = true;
            request.City.Eq = value;
            return true;
        }

        private bool CountryNull(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Country.IsActive = true;
            if (v == "1")
            {
                request.Country.IsNull = true;
            }
            else if (v == "0")
            {
                request.Country.IsNull = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool CountryEq(FilterRequest request, StringValues value)
        {
            request.Country.IsActive = true;
            request.Country.Eq = value;
            return true;
        }

        private bool PhoneNull(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Phone.IsActive = true;
            if (v == "1")
            {
                request.Phone.IsNull = true;
            }
            else if (v == "0")
            {
                request.Phone.IsNull = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool PhoneCode(FilterRequest request, StringValues value)
        {
            request.Phone.IsActive = true;
            short code;
            if (short.TryParse(value, out code))
            {
                request.Phone.Code = code;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool SnameNull(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Sname.IsActive = true;
            if (v == "1")
            {
                request.Sname.IsNull = true;
            }
            else if (v == "0")
            {
                request.Sname.IsNull = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool SnameStarts(FilterRequest request, StringValues value)
        {
            request.Sname.IsActive = true;
            request.Sname.Starts = value;
            return true;
        }

        private bool SnameEq(FilterRequest request, StringValues value)
        {
            request.Sname.IsActive = true;
            request.Sname.Eq = value;
            return true;
        }

        private bool FnameNull(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Fname.IsActive = true;
            if (v == "1")
            {
                request.Fname.IsNull = true;
            }
            else if (v == "0")
            {
                request.Fname.IsNull = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool FnameAny(FilterRequest request, StringValues value)
        {
            request.Fname.IsActive = true;
            request.Fname.Any = value;
            return true;
        }

        private bool FnameEq(FilterRequest request, StringValues value)
        {
            request.Fname.IsActive = true;
            request.Fname.Eq = value;
            return true;
        }

        private bool StatusNeq(FilterRequest request, StringValues value)
        {
            request.Status.IsActive = true;
            Status status;
            if (StatusHelper.TryParse(value, out status))
            {
                request.Status.Neq = status;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool StatusEq(FilterRequest request, StringValues value)
        {
            request.Status.IsActive = true;
            Status status;
            if (StatusHelper.TryParse(value, out status))
            {
                request.Status.Eq = status;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool EmailGt(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Email.IsActive = true;
            if (request.Email.Gt != null)
            {
                request.Email.Gt = (String.Compare(request.Email.Gt, v) > 0)
                    ? request.Email.Gt
                    : v;
            }
            else
            {
                request.Email.Gt = v;
            }

            return true;
        }

        private bool EmailLt(FilterRequest request, StringValues value)
        {
            string v = value.ToString();
            request.Email.IsActive = true;
            if (request.Email.Lt != null)
            {
                request.Email.Lt = (String.Compare(request.Email.Lt, v) < 0)
                    ? request.Email.Lt
                    : v;
            }
            else
            {
                request.Email.Lt = v;
            }

            return true;
        }

        private bool EmailDomain(FilterRequest request, StringValues value)
        {
            request.Email.IsActive = true;
            request.Email.Domain = value;
            return true;
        }

        private bool SexEq(FilterRequest request, StringValues value)
        {
            request.Sex.IsActive = true;
            if (value == "m")
            {
                request.Sex.IsMale = true;
            }
            else if (value == "f")
            {
                request.Sex.IsFemale = true;
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}