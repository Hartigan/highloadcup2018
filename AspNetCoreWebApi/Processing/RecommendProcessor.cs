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
    public class RecommendProcessor
    {
        private Subject<RecommendRequest> _dataRequest = new Subject<RecommendRequest>();
        private readonly MainContext _context;
        private readonly MainStorage _storage;
        private readonly MainPool _pool;
        private readonly RecommendPrinter _printer;

        public IObservable<RecommendRequest> DataRequest => _dataRequest;

        public RecommendProcessor(
            MainStorage mainStorage,
            MainContext mainContext,
            MainPool mainPool,
            RecommendPrinter printer
        )
        {
            _context = mainContext;
            _storage = mainStorage;
            _pool = mainPool;
            _printer = printer;
        }

        private void Free(RecommendRequest request)
        {
            _pool.RecommendRequest.Return(request);
        }

        public async Task<bool> Process(int id, HttpResponse httpResponse, IQueryCollection query)
        {
            RecommendRequest request = _pool.RecommendRequest.Get();
            request.Id = id;

            foreach (var filter in query)
            {
                bool res = true;
                switch(filter.Key)
                {
                    case "query_id":
                        break;

                    case "limit":
                        uint limit;
                        if (!uint.TryParse(filter.Value,  out limit))
                        {
                            res = false;
                        }
                        else
                        {
                            if (limit == 0)
                            {
                                res = false;
                            }
                            request.Limit = (int)limit;
                        }
                        break;

                    case "country":
                        res = CountryEq(request, filter.Value);
                        break;

                    case "city":
                        res = CityEq(request, filter.Value);
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

            _dataRequest.OnNext(request);

            var result = await request.TaskCompletionSource.Task;

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";
            using(var sw = new StreamWriter(httpResponse.Body))
            {
                _printer.Write(result, sw);
            }
            _pool.RecommendResponse.Return(result);
            Free(request);
            return true;
        }

        private bool CityEq(RecommendRequest request, StringValues value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return false;
            }

            request.City.IsActive = true;
            request.City.City = value;
            return true;
        }

        private bool CountryEq(RecommendRequest request, StringValues value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return false;
            }

            request.Country.IsActive = true;
            request.Country.Country = value;
            return true;
        }
    }
}