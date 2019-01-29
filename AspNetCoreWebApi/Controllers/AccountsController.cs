using System;
using System.Threading.Tasks;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreWebApi.Controllers
{
    public class AccountsController
    {
        private readonly NewAccountProcessor _newAccountProcessor;
        private readonly EditAccountProcessor _editAccountProcessor;
        private readonly NewLikesProcessor _newLikesProcessor;
        private readonly FilterProcessor _filterProcessor;
        private readonly GroupProcessor _groupProcessor;
        private readonly RecommendProcessor _recommendProcessor;
        private readonly SuggestProcessor _suggestProcessor;
        private readonly MainStorage _storage;

        public AccountsController(
            NewAccountProcessor newAccountProcessor,
            EditAccountProcessor editAccountProcessor,
            NewLikesProcessor newLikesProcessor,
            FilterProcessor filterProcessor,
            GroupProcessor groupProcessor,
            RecommendProcessor recommendProcessor,
            SuggestProcessor suggestProcessor,
            MainStorage mainStorage)
        {
            _newAccountProcessor = newAccountProcessor;
            _editAccountProcessor = editAccountProcessor;
            _newLikesProcessor = newLikesProcessor;
            _filterProcessor = filterProcessor;
            _groupProcessor = groupProcessor;
            _recommendProcessor = recommendProcessor;
            _suggestProcessor = suggestProcessor;
            _storage = mainStorage;
        }

        private static byte[] _postOk = new byte[2] { 123, 125 };

        private void WritePostOk(HttpResponse response)
        {
            response.ContentType = "application/json";
            response.ContentLength = 2;
            response.Body.Write(_postOk, 0, 2);
        }

        private Task SkipFailed(Action work, HttpResponse response)
        {
            long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            return Task.Run(() => {

                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - start > 1500)
                {
                    response.StatusCode = 400;
                    return;
                }

                work();
            });
        }

        public Task Create(HttpRequest request, HttpResponse response)
        {
            if (_newAccountProcessor.Process(request.Body))
            {
                response.StatusCode = 201;
                WritePostOk(response);
            }
            else
            {
                response.StatusCode = 400;
            }

            return Task.CompletedTask;
        }

        public Task Edit(HttpRequest request, HttpResponse response, string strId)
        {
            int id = 0;
            if (!int.TryParse(strId, out id))
            {
                response.StatusCode = 404;
                return Task.CompletedTask;
            }

            if (!_storage.Ids.Contains(id))
            {
                response.StatusCode = 404;
                return Task.CompletedTask;
            }

            if (_editAccountProcessor.Process(request.Body, id))
            {
                response.StatusCode = 202;
                WritePostOk(response);
            }
            else
            {
                response.StatusCode = 400;
            }

            return Task.CompletedTask;
        }

        public Task AddLikes(HttpRequest request, HttpResponse response)
        {
            if (_newLikesProcessor.Process(request.Body))
            {
                response.StatusCode = 202;
                WritePostOk(response);
            }
            else
            {
                response.StatusCode = 400;
            }

            return Task.CompletedTask;
        }

        public Task Filter(HttpRequest request, HttpResponse response)
        {
            return SkipFailed(() => {
                if (!_filterProcessor.Process(response, request.Query))
                {
                    response.StatusCode = 400;
                }
            }, response);
        }

        public Task Group(HttpRequest request, HttpResponse response)
        {
            return SkipFailed(() => {
                if (!_groupProcessor.Process(response, request.Query))
                {
                    response.StatusCode = 400;
                }
            }, response);
        }

        public Task Recommend(HttpRequest request, HttpResponse response, int id)
        {
            if (!_storage.Ids.Contains(id))
            {
                response.StatusCode = 404;
                return Task.CompletedTask;
            }

            return SkipFailed(() => {
                if (!_recommendProcessor.Process(id, response, request.Query))
                {
                    response.StatusCode = 400;
                }
            }, response);
        }

        public Task Suggest(HttpRequest request, HttpResponse response, int id)
        {
            if (!_storage.Ids.Contains(id))
            {
                response.StatusCode = 404;
                return Task.CompletedTask;
            }

            return SkipFailed(() => {
                if (!_suggestProcessor.Process(id, response, request.Query))
                {
                    response.StatusCode = 400;
                }
            }, response);
        }
    }
}
