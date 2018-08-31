/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.IdentityModel.Tokens.Jwt;
using WebApp_RoleClaims_DotNet.Utils;

namespace WebApp_RoleClaims_DotNet
{
    public partial class Startup
    {
        /// <summary>
        /// Configures OpenIDConnect Authentication & Adds Custom Application Authorization Logic on User Login for Multi-tenant scenario.
        /// </summary>
        /// <param name="app">The application represented by a <see cref="IAppBuilder"/> object.</param>
        public void ConfigureMultitenantAuth(IAppBuilder app)
        {
            // By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
            // 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' instead of 'roles'
            // This flag ensures that the ClaimsIdentity claims collection will be built from the claims in the token
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = "d43fe016-2ce2-4686-ad31-9169be137d72",  // This app is already registered as a multi-tenant app in the Microsoft tenant. The clientId provided in the web.config is ignored.
                    Authority = ConfigHelper.CommonAuthority,           // Use the 'common' endpoint for multi-tenant
                    RedirectUri = "https://localhost:44322/",
                    PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,

                    // Here, we've disabled issuer validation for the multi-tenant sample.  This enables users
                    // from ANY tenant to sign into the application (solely for the purposes of allowing the sample
                    // to be run out-of-the-box.  For a real multi-tenant app, reference the issuer validation in 
                    // WebApp-MultiTenant-OpenIDConnect-DotNet.  
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        RoleClaimType = "roles",
                        NameClaimType = "upn"
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Error/ShowError?signIn=true&errorMessage=" + context.Exception.Message);
                            return System.Threading.Tasks.Task.FromResult(0);
                        }
                    }
                });
        }
    }
}