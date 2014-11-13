using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Security.Claims;

//The following libraries were defined and added to this sample.
using WebApp_RoleClaims_DotNet.Utils;

namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// Sends an OpenIDConnect Sign-In Request.
        /// </summary>
        public void SignIn(string redirectUri)
        {
            if (redirectUri == null)
                redirectUri = "/";

            HttpContext.GetOwinContext()
                .Authentication.Challenge(new AuthenticationProperties {RedirectUri = redirectUri},
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        /// <summary>
        /// Signs the user out and clears the cache of access tokens.
        /// </summary>
        public void SignOut()
        {
            // Remove all cache entries for this user and send an OpenID Connect sign-out request.
            if (Request.IsAuthenticated)
            {
                Claim userObjectIdClaim = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType);
                if (userObjectIdClaim != null && !string.IsNullOrEmpty(ConfigHelper.Authority))
                {
                    var authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectIdClaim.Value));
                    authContext.TokenCache.Clear();
                }

                HttpContext.GetOwinContext().Authentication.SignOut(
                    OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
            }
        }
    }
}