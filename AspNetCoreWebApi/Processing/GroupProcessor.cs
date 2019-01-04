using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Printers;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AspNetCoreWebApi.Processing
{
    public class GroupProcessor
    {
        private Subject<GroupRequest> _dataRequest = new Subject<GroupRequest>();
        private readonly MainContext _context;
        private readonly MainStorage _storage;

        public IObservable<GroupRequest> DataRequest => _dataRequest;

        public GroupProcessor(
            MainStorage mainStorage,
            MainContext mainContext
        )
        {
            _context = mainContext;
            _storage = mainStorage;
        }

        public async Task<bool> Process(HttpResponse httpResponse, IQueryCollection query)
        {
            GroupRequest request = new GroupRequest();
            List<GroupKey> keys = new List<GroupKey>(5);

            foreach (var filter in query)
            {
                bool res = true;
                switch(filter.Key)
                {
                    case "query_id":
                        break;

                    case "limit":
                        int limit;
                        if (!int.TryParse(filter.Value,  out limit))
                        {
                            return false;
                        }
                        else
                        {
                            request.Limit = limit;
                        }
                        break;

                    case "order":
                        request.Order = filter.Value == "1";
                        break;

                    case "sex":
                        res = SexEq(request, filter.Value);
                        break;

                    case "status":
                        res = StatusEq(request, filter.Value);
                        break;

                    case "country":
                        res = CountryEq(request, filter.Value);
                        break;

                    case "city":
                        res = CityEq(request, filter.Value);
                        break;

                    case "birth":
                        res = BirthEq(request, filter.Value);
                        break;

                    case "interests":
                        res = InterestsEq(request, filter.Value);
                        break;

                    case "likes":
                        res = LikesEq(request, filter.Value);
                        break;

                    case "joined":
                        res = JoinedEq(request, filter.Value);
                        break;

                    case "keys":
                        foreach(var str in filter.Value.ToString().Split(','))
                        {
                            GroupKey key;
                            if (GroupKeyExtensions.TryParse(str, out key))
                            {
                                keys.Add(key);
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    default:
                        return false;
                }

                if (!res)
                {
                    return false;
                }
            }

            if (keys.Count == 0)
            {
                return false;
            }

            request.Keys = keys;

            _dataRequest.OnNext(request);

            var result = await request.TaskCompletionSource.Task;

            var printer = new GroupPrinter(_storage, _context);

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";
            using(var sw = new StreamWriter(httpResponse.Body))
            {
                printer.Write(result, sw);
            }

            return true;
        }

        private bool JoinedEq(GroupRequest request, StringValues value)
        {
            request.Joined.IsActive = true;
            int year;
            if (int.TryParse(value, out year))
            {
                request.Joined.Year = year;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool LikesEq(GroupRequest request, StringValues value)
        {
            request.Like.IsActive = true;
            int id;
            if (int.TryParse(value, out id))
            {
                request.Like.Id = id;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool InterestsEq(GroupRequest request, StringValues value)
        {
            request.Interest.IsActive = true;
            request.Interest.Interest = value;
            return true;
        }

        private bool BirthEq(GroupRequest request, StringValues value)
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

        private bool CityEq(GroupRequest request, StringValues value)
        {
            request.City.IsActive = true;
            request.City.City = value;
            return true;
        }

        private bool CountryEq(GroupRequest request, StringValues value)
        {
            request.Country.IsActive = true;
            request.Country.Country = value;
            return true;
        }

        private bool StatusEq(GroupRequest request, StringValues value)
        {
            request.Status.IsActive = true;
            Status status;
            if (StatusHelper.TryParse(value, out status))
            {
                request.Status.Status = status;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool SexEq(GroupRequest request, StringValues value)
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