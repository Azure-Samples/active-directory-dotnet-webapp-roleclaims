using Owin;
using System;
using System.Collections.Generic;

//The following libraries were added to this sample.
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;

//The following libraries were defined and added to this sample.
using WebApp_RoleClaims_DotNet.Utils;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;


namespace WebApp_RoleClaims_DotNet
{
    public partial class Startup
    {
        /// <summary>
        /// Configures OpenIDConnect Authentication & Adds Custom Application Authorization Logic on User Login.
        /// </summary>
        /// <param name="app">The application represented by a <see cref="IAppBuilder"/> object.</param>
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            //Configure OpenIDConnect, register callbacks for OpenIDConnect Notifications
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ConfigHelper.ClientId,
                    Authority = ConfigHelper.CommonAuthority,
                    PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthorizationCodeReceived = context =>
                        {
                            // Set Tenant-Dependent Configuration Values
                            ClaimsIdentity claimsId = context.AuthenticationTicket.Identity; 
                            string tenantId = claimsId.FindFirst(Globals.TenantIdClaimType).Value;
                            ConfigHelper.Authority = String.Format(CultureInfo.InvariantCulture, ConfigHelper.AadInstance, tenantId);
                            ConfigHelper.GraphServiceRoot = new Uri (ConfigHelper.GraphResourceId + tenantId);

                            // Get Access Token for User's Directory
                            try
                            {
                                string userObjectId = claimsId.FindFirst(Globals.ObjectIdClaimType).Value;
                                ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                                AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
                                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                                    context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, ConfigHelper.GraphResourceId);
                            }
                            catch (AdalException e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                            }

                            foreach (Claim claim in claimsId.Claims)
                                claimsId.AddClaim(new Claim(ClaimTypes.Role, claim.Value, ClaimValueTypes.String, "WebApp_RoleClaims_DotNet"));

                            return Task.FromResult(0);
                        },

                        RedirectToIdentityProvider = context =>
                        {
                            if (context.Request.Path.Value.Equals("/Account/SignIn") && context.Request.Query.Equals("admin_consent=true"))
                                context.ProtocolMessage.Prompt = "admin_consent";
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}