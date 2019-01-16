using System;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Storage;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreWebApi.Controllers
{
    [ApiController]
    public class AccountsController : ControllerBase
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

        [Route("accounts/new")]
        [HttpPost]
        public ActionResult Create()
        {
            if (_newAccountProcessor.Process(Request.Body))
            {
                Response.StatusCode = 201;
                Response.ContentType = "application/json";
                return Content("{}");
            }
            else
            {
                Response.StatusCode = 400;
                return Content(String.Empty);
            }
        }

        [Route("accounts/{strId}")]
        [HttpPost]
        public ActionResult Edit(string strId)
        {
            int id = 0;
            if (!int.TryParse(strId, out id))
            {
                Response.StatusCode = 404;
                return Content(String.Empty);
            }

            if (!_storage.Ids.Contains(id))
            {
                Response.StatusCode = 404;
                return Content(String.Empty);
            }

            if (_editAccountProcessor.Process(Request.Body, id))
            {
                Response.StatusCode = 202;
                Response.ContentType = "application/json";
                return Content("{}");
            }
            else
            {
                Response.StatusCode = 400;
                return Content(String.Empty);
            }
        }

        [Route("accounts/likes")]
        [HttpPost]
        public ActionResult AddLikes()
        {
            if (_newLikesProcessor.Process(Request.Body))
            {
                Response.StatusCode = 202;
                Response.ContentType = "application/json";
                return Content("{}");
            }
            else
            {
                Response.StatusCode = 400;
                return Content(String.Empty);
            }
        }

        [Route("accounts/filter")]
        [HttpGet]
        public void Filter()
        {
            if (!_filterProcessor.Process(Response, Request.Query))
            {
                Response.StatusCode = 400;
            }
        }

        [Route("accounts/group")]
        [HttpGet]
        public void Group()
        {
            if (!_groupProcessor.Process(Response, Request.Query))
            {
                Response.StatusCode = 400;
            }
        }

        [Route("accounts/{id}/recommend")]
        [HttpGet]
        public void Recommend(int id)
        {
            if (!_storage.Ids.Contains(id))
            {
                Response.StatusCode = 404;
                return;
            }

            if (!_recommendProcessor.Process(id, Response, Request.Query))
            {
                Response.StatusCode = 400;
            }
        }

        [Route("accounts/{id}/suggest")]
        [HttpGet]
        public void Suggest(int id)
        {
            if (!_storage.Ids.Contains(id))
            {
                Response.StatusCode = 404;
                return;
            }

            if (!_suggestProcessor.Process(id, Response, Request.Query))
            {
                Response.StatusCode = 400;
            }
        }
    }
}
