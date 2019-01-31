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
        private readonly GroupPreprocessor _groupsPreprocessor;

        public AccountsController(
            GroupPreprocessor groupsPreprocessor,
            NewAccountProcessor newAccountProcessor,
            EditAccountProcessor editAccountProcessor,
            NewLikesProcessor newLikesProcessor,
            FilterProcessor filterProcessor,
            GroupProcessor groupProcessor,
            RecommendProcessor recommendProcessor,
            SuggestProcessor suggestProcessor,
            MainStorage mainStorage)
        {
            _groupsPreprocessor = groupsPreprocessor;
            _newAccountProcessor = newAccountProcessor;
            _editAccountProcessor = editAccountProcessor;
            _newLikesProcessor = newLikesProcessor;
            _filterProcessor = filterProcessor;
            _groupProcessor = groupProcessor;
            _recommendProcessor = recommendProcessor;
            _suggestProcessor = suggestProcessor;
            _storage = mainStorage;
        }

        private void WritePostOk(HttpResponse response)
        {
            response.ContentType = "application/json";
            response.ContentLength = 2;
            response.Body.WriteByte(123);
            response.Body.WriteByte(125);
        }

        private Task SkipFailed(Action work, HttpResponse response, bool enabled)
        {
            if (enabled)
            {
                long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                return Task.Run(() => {

                    if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - start > 200)
                    {
                        response.StatusCode = 400;
                        return;
                    }

                    work();
                });
            }
            else
            {
                return Task.Run(work);
            }
        }

        public Task Create(HttpRequest request, HttpResponse response)
        {
            return Task.Run(() => {

                if (_newAccountProcessor.Process(request.Body))
                {
                    response.StatusCode = 201;
                    WritePostOk(response);
                }
                else
                {
                    response.StatusCode = 400;
                }
            });
        }

        public Task Edit(HttpRequest request, HttpResponse response, string strId)
        {
            int id = 0;
            if (!int.TryParse(strId, out id))
            {
                response.StatusCode = 404;
                return Task.CompletedTask;
            }

            return Task.Run(() => {
                if (!_storage.Ids.Contains(id))
                {
                    response.StatusCode = 404;
                    return;
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
            });
        }

        public Task AddLikes(HttpRequest request, HttpResponse response)
        {
            return Task.Run(() => {

                if (_newLikesProcessor.Process(request.Body))
                {
                    response.StatusCode = 202;
                    WritePostOk(response);
                }
                else
                {
                    response.StatusCode = 400;
                }
            });
        }

        public Task Filter(HttpRequest request, HttpResponse response)
        {
            return SkipFailed(() => {
                if (!_filterProcessor.Process(response, request.Query))
                {
                    response.StatusCode = 400;
                }
            },
            response,
            _groupsPreprocessor.IndexRemoved);
        }

        public Task Group(HttpRequest request, HttpResponse response)
        {
            if (_groupsPreprocessor.IndexRemoved)
            {
                response.StatusCode = 400;
                return Task.CompletedTask;
            }

            return SkipFailed(() => {
                if (!_groupProcessor.Process(response, request.Query))
                {
                    response.StatusCode = 400;
                }
            },
            response,
            _groupsPreprocessor.IndexRemoved);
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
            },
            response,
            _groupsPreprocessor.IndexRemoved);
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
            },
            response,
            _groupsPreprocessor.IndexRemoved);
        }
    }
}
