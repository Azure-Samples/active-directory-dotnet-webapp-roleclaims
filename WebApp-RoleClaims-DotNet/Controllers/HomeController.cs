using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web.Mvc;

namespace WebApp_RoleClaims_DotNet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app roles.";

            ClaimsIdentity claimsId = ClaimsPrincipal.Current.Identity as ClaimsIdentity;
            var appRoles = new List<String>();
            foreach (Claim claim in ClaimsPrincipal.Current.FindAll("roles"))
                appRoles.Add(claim.Value);
            ViewData["appRoles"] = appRoles;

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}