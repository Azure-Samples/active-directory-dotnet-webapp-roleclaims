// #define SingleTenantApp
using System;
using System.Globalization;

//The following namespace was added to this sample.
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

//The following namespace was defined and added to this sample.
using WebApp_RoleClaims_DotNet.Utils;


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
#if SingleTenantApp
                    Authority = String.Format(CultureInfo.InvariantCulture, ConfigHelper.AadInstance, ConfigHelper.Tenant), // For Single-Tenant
#else
                    Authority = ConfigHelper.CommonAuthority, // For Multi-Tenant
#endif
                    PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,

                    // Here, we've disabled issuer validation for the multi-tenant sample.  This enables users
                    // from ANY tenant to sign into the application (solely for the purposes of allowing the sample
                    // to be run out-of-the-box.  For a real multi-tenant app, reference the issuer validation in 
                    // WebApp-MultiTenant-OpenIDConnect-DotNet.  If you're running this sample as a single-tenant
                    // app, you can delete the ValidateIssuer property below.
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
#if !SingleTenantApp
                        ValidateIssuer = false, // For Multi-Tenant Only
#endif
                        RoleClaimType = "roles",
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Error/ShowError?signIn=true&errorMessage=" + context.Exception.Message);
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}