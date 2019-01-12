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
    public class SuggestProcessor
    {
        private Subject<SuggestRequest> _dataRequest = new Subject<SuggestRequest>();
        private readonly MainContext _context;
        private readonly MainStorage _storage;
        private readonly MainPool _pool;
        private readonly SuggestPrinter _printer;

        public IObservable<SuggestRequest> DataRequest => _dataRequest;

        public SuggestProcessor(
            MainStorage mainStorage,
            MainContext mainContext,
            MainPool mainPool,
            SuggestPrinter printer
        )
        {
            _context = mainContext;
            _storage = mainStorage;
            _pool = mainPool;
            _printer = printer;
        }

        private void Free(SuggestRequest request)
        {
            _pool.SuggestRequest.Return(request);
        }

        public async Task<bool> Process(int id, HttpResponse httpResponse, IQueryCollection query)
        {
            SuggestRequest request = _pool.SuggestRequest.Get();
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
            _pool.SuggestResponse.Return(result);
            Free(request);
            return true;
        }

        private bool CityEq(SuggestRequest request, StringValues value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            request.City.IsActive = true;
            request.City.City = value;
            return true;
        }

        private bool CountryEq(SuggestRequest request, StringValues value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            request.Country.IsActive = true;
            request.Country.Country = value;
            return true;
        }
    }
}