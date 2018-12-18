using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IdStorage _idStorage;

        public AccountsController(
            NewAccountProcessor newAccountProcessor,
            EditAccountProcessor editAccountProcessor,
            IdStorage idStorage)
        {
            _newAccountProcessor = newAccountProcessor;
            _editAccountProcessor = editAccountProcessor;
            _idStorage = idStorage;
        }


        [Route("accounts/filter")]
        [HttpGet]
        public ActionResult<string> filter()
        {
            return "Hello";
        }

        [Route("accounts/new")]
        [HttpPost]
        public ActionResult create()
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

        [Route("accounts/{id}")]
        [HttpPost]
        public ActionResult edit(int id)
        {
            if (!_idStorage.Contains(id))
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
    }
}
