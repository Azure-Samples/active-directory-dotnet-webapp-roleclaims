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
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;


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

                    // Here, we've disabled issuer validation for the multi-tenant sample.  This enables users
                    // from ANY tenant to sign into the application (solely for the purposes of allowing the sample
                    // to be run out-of-the-box.  For a real multi-tenant app, see the issuer validation in 
                    // WebApp-MultiTenant-OpenIDConnect-DotNet.  If you're running this sample as a single-tenant
                    // app, you can delete the following 4 lines.
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = false,
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthorizationCodeReceived = async context =>
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
                            catch (AdalException)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                            }

                            // Add MVC-Specific Role Claims for each AAD Role Claim Received
                            foreach (Claim claim in claimsId.FindAll("roles"))
                                claimsId.AddClaim(new Claim(ClaimTypes.Role, claim.Value, ClaimValueTypes.String, "WebApp_RoleClaims_DotNet"));

                            // Add Application Owners as Admins (Temporary, see below)
                            try {
                                await AddOwnerAdminClaim(claimsId);
                            }
                            catch (Exception e)
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error/ShowError?errorMessage=Were having trouble signing you in&signIn=true");
                            }

                            return;
                        }
                    }
                });
        }

        // This method is included temporarily to enable the use of Role Claims with Single-Tenant Applications.
        // It will be removed as soon as the Azure Portal UI supports Role Assignments on Single-Tenant Applications.
        // If you are running this sample as a multi-tenant application, you can remove this method entirely and simply
        // assign a user to the Admin role in the Azure Portal.
        private async Task AddOwnerAdminClaim(ClaimsIdentity claimsId)
        {
            string userObjectId = claimsId.FindFirst(Globals.ObjectIdClaimType).Value;
            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () =>
            {
                ClientCredential cred = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, cred, new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                return result.AccessToken;
            });
            IPagedCollection<IApplication> tenantApps = await graphClient.Applications.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
            if (tenantApps.CurrentPage.Count == 0)
                return;
            IApplicationFetcher appFetcher = (IApplicationFetcher)tenantApps.CurrentPage[0];
            IPagedCollection<IDirectoryObject> appOwners = await appFetcher.Owners.Where(o => o.ObjectId.Equals(userObjectId)).ExecuteAsync();
            if (appOwners.CurrentPage.Count > 0)
                claimsId.AddClaim(new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String, "WebApp_RoleClaims_DotNet"));
        }
    }
}