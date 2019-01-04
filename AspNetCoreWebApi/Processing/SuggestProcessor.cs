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
    public class SuggestProcessor
    {
        private Subject<SuggestRequest> _dataRequest = new Subject<SuggestRequest>();
        private readonly MainContext _context;
        private readonly MainStorage _storage;

        public IObservable<SuggestRequest> DataRequest => _dataRequest;

        public SuggestProcessor(
            MainStorage mainStorage,
            MainContext mainContext
        )
        {
            _context = mainContext;
            _storage = mainStorage;
        }

        public async Task<bool> Process(int id, HttpResponse httpResponse, IQueryCollection query)
        {
            SuggestRequest request = new SuggestRequest();
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
                            return false;
                        }
                        else
                        {
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
                        return false;
                }

                if (!res)
                {
                    return false;
                }
            }

            _dataRequest.OnNext(request);

            var result = await request.TaskCompletionSource.Task;

            var printer = new SuggestPrinter(_storage, _context);

            httpResponse.StatusCode = 200;
            httpResponse.ContentType = "application/json";
            using(var sw = new StreamWriter(httpResponse.Body))
            {
                printer.Write(result, sw);
            }

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