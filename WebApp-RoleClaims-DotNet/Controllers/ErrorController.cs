using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult ShowError(string errorMessage, string signIn)
        {
            ViewBag.SignIn = signIn;
            ViewBag.ErrorMessage = errorMessage;
            return View();
        }

        public ActionResult ReAuth(string redirectUri)
        {
            ViewBag.RedirectUri = redirectUri;
            return View();
        }
    }
}