using System.Web.Mvc;
using System.Web;

namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class ErrorController : Controller
    {
        /// <summary>
        ///     Shows an on-screen error message when the user attemps various
        ///     illegal actions.
        /// </summary>
        /// <returns>Generic error <see cref="View" />.</returns>
        public ActionResult ShowError(string errorMessage, string signIn)
        {
            ViewBag.SignIn = signIn;
            ViewBag.ErrorMessage = errorMessage;
            return View();
        }

        public ActionResult Reauth(string redirectUri)
        {
            ViewBag.RedirectUri = redirectUri;
            return View();
        }
    }
}