using System;
using System.Collections.Generic;
//The following libraries were added to this sample.
using System.Configuration;
using System.Globalization;

namespace WebApp_RoleClaims_DotNet.Utils
{
    public static class Globals
    {
        private static string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string tenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private static List<String> taskStatuses = new List<String>(new String[4] { "Not  Started", "In Progress", "Complete", "Blocked" });

        internal static string ObjectIdClaimType { get { return objectIdClaimType; } }
        internal static string TenantIdClaimType { get { return tenantIdClaimType; } }
        public static List<String> Statuses { get { return taskStatuses; } }
    }
}