using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace WebApp_RoleClaims_DotNet.Utils
{
    public class ConfigHelper
    {
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        // The GraphResourceId the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
        // The GraphApiVersion specifies which version of the AAD Graph API to call.
        // The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        // The Authority is the sign-in URL of the tenant.

        private static readonly string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string graphResourceId = ConfigurationManager.AppSettings["ida:GraphUrl"];
        private static string appTenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string graphApiVersion = ConfigurationManager.AppSettings["ida:GraphApiVersion"];
        private static readonly string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        private static string commonAuthority = String.Format(CultureInfo.InvariantCulture, aadInstance, "common/");

        public static string ClientId { get { return clientId; } }
        internal static string AppKey { get { return appKey; } }
        internal static string GraphResourceId { get { return graphResourceId; } }
        internal static string GraphApiVersion { get { return graphApiVersion; } }
        internal static string AadInstance { get { return aadInstance; } }
        internal static string PostLogoutRedirectUri { get { return postLogoutRedirectUri; } }
        internal static string CommonAuthority { get { return commonAuthority; } }
        internal static string Authority { get; set; }
        internal static string Tenant { get { return appTenant; } }
        internal static Uri GraphServiceRoot { get; set; }

    }
}