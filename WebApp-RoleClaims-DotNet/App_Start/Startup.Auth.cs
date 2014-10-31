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
                        AuthorizationCodeReceived = async context =>
                        {
                            ClaimsIdentity claimsId = context.AuthenticationTicket.Identity; 
                            string userObjectId = claimsId.FindFirst(Globals.ObjectIdClaimType).Value;
                            ConfigHelper.Authority = String.Format(CultureInfo.InvariantCulture, ConfigHelper.AadInstance, claimsId.FindFirst(Globals.TenantIdClaimType).Value);
                            ConfigHelper.GraphServiceRoot = new Uri (ConfigHelper.GraphResourceId + claimsId.FindFirst(Globals.TenantIdClaimType).Value);

                            try
                            {
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

                            try
                            {
                                if (claimsId.FindFirst("_claim_sources") != null)
                                    await AddGroupClaimsFromGraphAPI(claimsId, userObjectId); //TODO: Can I run this async?
                            }
                            catch (Exception e)
                            {
                                claimsId.AddClaim(new Claim("groups", "error", ClaimValueTypes.String));
                            }


                            return;
                        }

                        //RedirectToIdentityProvider = context => {
                        //    context.ProtocolMessage.Prompt = "admin_consent";
                        //    return Task.FromResult(0);
                        //}
                    }
                });
        }

        /// <summary>
        /// We must query the GraphAPI to obtain information about the user and the security groups they are a member of.
        /// Here we use the GraphAPI Client Library to do so.
        /// </summary>
        private async Task AddGroupClaimsFromGraphAPI(ClaimsIdentity claimsIdentity, string userObjectId)
        {
            // Acquire the Access Token
            ClientCredential credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
            AuthenticationResult result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential,
                new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

            // Get the GraphAPI Group Endpoint for the specific user from the _claim_sources claim in token
            string namesJSON = claimsIdentity.FindFirst("_claim_sources").Value;
            ClaimSource source = JsonConvert.DeserializeObject<ClaimSource>(namesJSON);
            string requestUrl = String.Format(CultureInfo.InvariantCulture, HttpUtility.HtmlEncode(source.src1.endpoint
                + "?api-version=" + ConfigHelper.GraphApiVersion));

            // Prepare and Make the POST request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            StringContent content = new StringContent("{\"securityEnabledOnly\": \"true\"}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;
            HttpResponseMessage response = await client.SendAsync(request);

            // Endpoint returns JSON with an array of Group ObjectIDs
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                GroupResponse groups = JsonConvert.DeserializeObject<GroupResponse>(responseContent);

                // For each Group, add its Object ID to the ClaimsIdentity as a Group Claim
                foreach (string groupObjectID in groups.value)
                    claimsIdentity.AddClaim(new Claim("groups", groupObjectID, ClaimValueTypes.String, "AAD-Tenant-Security-Groups")); //TODO type?

                return;
            }
            else
            {
                throw new WebException();
            }
        }

        private class ClaimSource
        {
            public Endpoint src1 { get; set; }

            public class Endpoint
            {
                public string endpoint { get; set; }
            }
        }

        private class GroupResponse
        {
            public string metadata { get; set; }
            public List<string> value { get; set; }
        }
    }
}