using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreWebApi.Processing;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreWebApi.Controllers
{
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly NewAccountProcessor _newAccountProcessor;

        public AccountsController(
            NewAccountProcessor newAccountProcessor)
        {
            _newAccountProcessor = newAccountProcessor;
        }


        [Route("accounts/filter")]
        [HttpGet]
        public ActionResult<string> filter()
        {
            return "Hello";
        }

        [Route("accounts/new")]
        [HttpPost]
        public ActionResult createNew()
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
    }
}
