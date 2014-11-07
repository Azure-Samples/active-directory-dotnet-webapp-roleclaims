using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebApp_RoleClaims_DotNet.Utils;
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Net;
using System.Linq.Expressions;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System.Threading.Tasks;


//The following libraries were defined and added to this sample.



namespace WebApp_RoleClaims_DotNet.Controllers
{
    
    public class HomeController : Controller
    {
        /// <summary>
        /// Shows the generic MVC Get Started Home Page. Allows unauthenticated
        /// users to see the home page and click the sign-in link.
        /// </summary>
        /// <returns>Generic Home <see cref="View"/>.</returns>

        public ActionResult Index()
        {          
            return View();
        }

        /// <summary>
        /// Gets user specific RBAC information: The Security Groups the user belongs to
        /// And the application roles the user has been granted.
        /// </summary>
        /// <returns>The About <see cref="View"/>.</returns>
        [Authorize]
        public ActionResult About()
        {
            var appRoles = new List<String>();
            foreach (Claim claim in ClaimsPrincipal.Current.FindAll(ClaimTypes.Role))
                appRoles.Add(claim.Value);
            ViewData["appRoles"] = appRoles;
            return View();
        }
    }
}