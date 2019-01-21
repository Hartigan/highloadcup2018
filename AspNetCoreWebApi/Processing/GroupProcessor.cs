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
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AspNetCoreWebApi.Processing
{
    public class GroupProcessor
    {
        private readonly MainContext _context;
        private readonly MainStorage _storage;
        private readonly MainPool _pool;
        private readonly GroupPrinter _printer;
        private readonly MessageProcessor _processor;

        public GroupProcessor(
            MainStorage mainStorage,
            MainContext mainContext,
            MainPool mainPool,
            GroupPrinter printer,
            MessageProcessor processor
        )
        {
            _context = mainContext;
            _storage = mainStorage;
            _pool = mainPool;
            _printer = printer;
            _processor = processor;
        }

        private void Free(GroupRequest request)
        {
            _pool.GroupRequest.Return(request);
        }

        public bool Process(HttpResponse httpResponse, IQueryCollection query)
        {
            GroupRequest request = _pool.GroupRequest.Get();

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
                            res = false;
                        }
                        else
                        {
                            if (limit <= 0)
                            {
                                res = false;
                            }
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
                                request.Keys |= key;
                                request.KeyOrder.Add(key);
                            }
                            else
                            {
                                res = false;
                            }
                        }

                        break;

                    default:
                        res = false;
                        break;
                }

                if (!res)
                {
                    Free(request);
                    return false;
                }
            }

            if (request.Keys == GroupKey.Empty)
            {
                Free(request);
                return false;
            }

            var result = _processor.Group(request);

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";

            var buffer = _pool.WriteBuffer.Get();
            int contentLength = 0;
            using(var bufferStream = new MemoryStream(buffer))
            using(var sw = new StreamWriter(bufferStream))
            {
                _printer.Write(result, sw);
                sw.Flush();
                httpResponse.ContentLength = contentLength = (int)bufferStream.Position;
            }

            httpResponse.Body.Write(buffer, 0, contentLength);
            _pool.WriteBuffer.Return(buffer);

            _pool.GroupResponse.Return(result);
            Free(request);
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