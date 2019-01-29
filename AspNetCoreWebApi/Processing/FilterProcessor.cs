using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
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

        private readonly MainPool _pool;

        private readonly AccountPrinter _printer;

        private readonly MessageProcessor _processor;

        public FilterProcessor(
            MainStorage mainStorage,
            MainContext mainContext,
            MainPool mainPool,
            AccountPrinter accountPrinter,
            MessageProcessor processor
        )
        {
            _pool = mainPool;
            _storage = mainStorage;
            _context = mainContext;
            _printer = accountPrinter;
            _processor = processor;
        }

        private void Free(FilterRequest request)
        {
            _pool.FilterRequest.Return(request);
        }

        public bool Process(HttpResponse httpResponse, IQueryCollection query)
        {
            if (DataConfig.DataUpdates)
            {
                return false;
            }

            FilterRequest request = _pool.FilterRequest.Get();

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
                            result = false;
                        }
                        else
                        {
                            if (limit <= 0)
                            {
                                result = false;
                            }
                            request.Limit = limit;
                        }
                        break;

                    case "sex_eq":
                        result = SexEq(request, filter.Value);
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
                        break;

                    case "status_neq":
                        result = StatusNeq(request, filter.Value);
                        break;

                    case "fname_eq":
                        result = FnameEq(request, filter.Value);
                        break;

                    case "fname_any":
                        result = FnameAny(request, filter.Value.ToString().Split(','));
                        break;

                    case "fname_null":
                        result = FnameNull(request, filter.Value);
                        break;

                    case "sname_eq":
                        result = SnameEq(request, filter.Value);
                        break;

                    case "sname_starts":
                        result = SnameStarts(request, filter.Value);
                        break;

                    case "sname_null":
                        result = SnameNull(request, filter.Value);
                        break;

                    case "phone_code":
                        result = PhoneCode(request, filter.Value);
                        break;

                    case "phone_null":
                        result = PhoneNull(request, filter.Value);
                        break;

                    case "country_eq":
                        result = CountryEq(request, filter.Value);
                        break;

                    case "country_null":
                        result = CountryNull(request, filter.Value);
                        break;

                    case "city_eq":
                        result = CityEq(request, filter.Value);
                        break;

                    case "city_any":
                        result = CityAny(request, filter.Value.ToString().Split(','));
                        break;

                    case "city_null":
                        result = CityNull(request, filter.Value);
                        break;

                    case "birth_lt":
                        result = BirthLt(request, filter.Value);
                        break;

                    case "birth_gt":
                        result = BirthGt(request, filter.Value);
                        break;

                    case "birth_year":
                        result = BirthYear(request, filter.Value);
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
                        break;

                    case "premium_null":
                        result = PremiumNull(request, filter.Value);
                        break;

                    default:
                        result = false;
                        break;
                }
                if (!result)
                {
                    Free(request);
                    return false;
                }
            }

            if (request.Likes.IsActive && DataConfig.LikesUpdates)
            {
                Free(request);
                return false;
            }

            if (request.Sex.IsActive)
            {
                request.Fields.Add(Field.Sex);
            }

            if (request.Status.IsActive)
            {
                request.Fields.Add(Field.Status);
            }

            if (request.Fname.IsActive)
            {
                request.Fields.Add(Field.FName);
            }

            if (request.Sname.IsActive)
            {
                request.Fields.Add(Field.SName);
            }

            if (request.Phone.IsActive)
            {
                request.Fields.Add(Field.Phone);
            }

            if (request.Country.IsActive)
            {
                request.Fields.Add(Field.Country);
            }

            if (request.City.IsActive)
            {
                request.Fields.Add(Field.City);
            }

            if (request.Birth.IsActive)
            {
                request.Fields.Add(Field.Birth);
            }

            if (request.Premium.IsActive)
            {
                request.Fields.Add(Field.Premium);
            }

            var response = _processor.Filter(request);

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";

            var buffer = _pool.WriteBuffer.Get();
            int contentLength = 0;
            using(var bufferStream = new MemoryStream(buffer))
            {
                _printer.Write(response, bufferStream, request.Fields);
                httpResponse.ContentLength = contentLength = (int)bufferStream.Position;
            }

            httpResponse.Body.Write(buffer, 0, contentLength);
            _pool.WriteBuffer.Return(buffer);

            _pool.FilterResponse.Return(response);
            Free(request);
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
            foreach(var v in value)
            {
                int id;
                if (int.TryParse(v, out id))
                {
                    request.Likes.Contains.Add(id);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool InterestsAny(FilterRequest request, StringValues value)
        {
            request.Interests.IsActive = true;
            request.Interests.Any.AddRange(value);
            return true;
        }

        private bool InterestsContains(FilterRequest request, StringValues value)
        {
            request.Interests.IsActive = true;
            request.Interests.Contains.AddRange(value);
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
                var gtOffset = new UnixTime(gt);
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
                var ltOffset = new UnixTime(lt);
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
            request.City.Any.UnionWith(value);
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
            request.Fname.Any.AddRange(value);
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