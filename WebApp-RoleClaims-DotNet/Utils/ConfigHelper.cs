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

using System;
using System.Configuration;
using System.Globalization;

namespace WebApp_RoleClaims_DotNet.Utils
{
    public class ConfigHelper
    {
        /// <summary>
        /// The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        /// </summary>
        public static string AADInstance { get; } = Util.EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);

        /// <summary>
        /// The Client ID is used by the application to uniquely identify itself to Azure AD.
        /// </summary>
        public static string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];

        /// <summary>
        /// The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        /// </summary>
        public static string PostLogoutRedirectUri { get; } = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        /// <summary>
        /// The TenantId is the DirectoryId of the Azure AD tenant being used in the sample
        /// </summary>
        public static string TenantId { get; } = ConfigurationManager.AppSettings["ida:TenantId"];

        /// <summary>
        /// The Authority is the sign-in URL of the tenant.
        /// </summary>
        public static string Authority = String.Format(CultureInfo.InvariantCulture, AADInstance, TenantId) + "/" ;

        /// <summary>
        /// The Azure AD 'common' endpoint to authenticate users for multi-tenant applications.
        /// </summary>
        public static string CommonAuthority = String.Format(CultureInfo.InvariantCulture, AADInstance, "common/");
    }
}