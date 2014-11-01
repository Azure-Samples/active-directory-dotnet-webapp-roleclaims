using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApp_RoleClaims_DotNet.Utils;

//The following libraries were added to this sample.


//The following libraries were defined and added to this sample.



namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class RolesController : Controller
    {
        ////////////////////////////////////////////////////////////////////
        //////// ACTIONS
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Show the current mappings of users and groups to each application role.
        /// Use AuthorizeAttribute to ensure only the role "Admin" can access the page.
        /// </summary>
        /// <returns>Role <see cref="View"/> with inputs to edit application role mappings.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Index()
        {
            Dictionary<string, List<IUserGroup>> roleAssignments = new Dictionary<string,List<IUserGroup>>();
            List<KeyValuePair<string, string>> appRoleGuids;

            // Get All Roles for the Application
            try
            {
                appRoleGuids = await GetApplicationRoles();
                foreach (KeyValuePair<string, string> kvp in appRoleGuids)
                    roleAssignments[kvp.Key] = new List<IUserGroup>();
            }
            catch (Exception e)
            {
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting application roles." });                
            }

            // Get an Access Token for the User's Directory
            UserQueryResult users;
            GroupQueryResult groups;
            try
            {
                users = await GetAllUsers();
                groups = await GetAllGroups(); // TODO: Multiple Threads
            }
            catch (AdalException e)
            {
                // If the user doesn't have an access token, they need to re-authorize
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while acquiring token." });
            }
            catch (Exception e)
            { 
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting users." });                
            }

            // foreach user, and for each group
                // batch request to the graph for appRoleAssignments
                // for each role assignment
                    // record user/group in result dictionary

            //ViewData["mappings"] = mappings;
            //ViewData["nameDict"] = nameDict;
            //ViewData["roles"] = Globals.Roles;
            //ViewData["host"] = Request.Url.AbsoluteUri;
            //ViewData["token"] = result.AccessToken;
            //ViewData["tenant"] = ConfigHelper.Tenant;
            return View();
        }


        ///// <summary>
        ///// Adds a User/Group<-->Application Role mapping from user input form
        ///// to roles.xml if it does not already exist.
        ///// </summary>
        ///// <param name="formCollection">The user input form, containing the UPN or GroupName
        ///// of the object to grant a role.</param>
        ///// <returns>A Redirect to the Roles page.</returns>
        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public ActionResult AssignRole(FormCollection formCollection)
        //{
        //    // Check for an input name
        //    if (formCollection != null && formCollection["id"].Length > 0)
        //    {
        //        //Get the Access Token for Calling Graph API from the cache
        //        AuthenticationResult result = null;
        //        try
        //        {
        //            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
        //            var authContext = new AuthenticationContext(ConfigHelper.Authority,
        //                new TokenDbCache(userObjectId));
        //            var credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
        //            result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential,
        //                new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
        //        }
        //        catch (AdalException e)
        //        {
        //                // The user needs to re-authorize.  Show them a message to that effect.
        //                return RedirectToAction("Index", "Roles", null);  
        //        }

        //        RolesDbHelper.AddRoleMapping(formCollection["id"], formCollection["role"]);
        //    }

        //    return RedirectToAction("Index", "Roles", null);
        //}


        ///// <summary>
        ///// Removes a ObjectID<-->Application Role mapping from Roles.xml, based on input
        ///// from the user.
        ///// </summary>
        ///// <param name="formCollection">The input from the user.</param>
        ///// <returns>A redirect to the roles page.</returns>
        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public ActionResult RemoveRole(FormCollection formCollection)
        //{
        //    // Remove role mapping assignments marked by checkboxes
        //    foreach (string key in formCollection.Keys)
        //    {
        //        if (formCollection[key].Equals("delete"))
        //            RolesDbHelper.RemoveRoleMapping(Convert.ToInt32(key));
        //    }
        //    return RedirectToAction("Index", "Roles", null);
        //}

        ///// <summary>
        ///// Used for the AadPickerLibrary that is used to search for users and groups.  Accepts a user input
        ///// and a number of results to retreive, and queries the graphAPI for possbble matches.
        ///// </summary>
        ///// <returns>JSON containing query results ot the Javascript library.</returns>
        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public async System.Threading.Tasks.Task<ActionResult> Search(string query, string token)
        //{
        //    // Search for users based on user input.
        //    try
        //    {
        //        HttpClient client = new HttpClient();
        //        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query);
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        HttpResponseMessage response = await client.SendAsync(request);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            return this.Content(await response.Content.ReadAsStringAsync());
        //        }
        //        else
        //        {
        //            return Json(new { error = "graph api error" }, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { error = "internal server error" }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        //////////////////////////////////////////////////////////////////////
        ////////// HELPER FUNCTIONS
        //////////////////////////////////////////////////////////////////////
        //#region HelperFunctions
        ///// <summary>
        ///// Queries the GraphAPI to get the DisplayName of a Group or User from its ObjectID.
        ///// </summary>
        ///// <param name="accessToken">The OpenIDConnect access token used 
        ///// to query the GraphAPI</param>
        ///// <param name="objectId">The ObjectID of the User or Group</param>
        ///// <returns>The DisplayName.</returns>
        //private static string GetDisplayNameFromObjectId(string accessToken, string objectId)
        //{
        //    // Setup Graph API connection
        //    Guid ClientRequestId = Guid.NewGuid();
        //    var graphSettings = new GraphSettings();
        //    graphSettings.ApiVersion = ConfigHelper.GraphApiVersion;
        //    var graphConnection = new GraphConnection(accessToken, ClientRequestId, graphSettings);

        //    try
        //    {
        //        // Get a User by ObjectID
        //        return graphConnection.Get<User>(objectId).DisplayName;
        //    }
        //    catch (GraphException e)
        //    {
        //        if (e.HttpStatusCode == HttpStatusCode.NotFound)
        //        {
        //            try
        //            {
        //                // If the User with ObjectID DNE, Get a group with the ObjectID
        //                return graphConnection.Get<Group>(objectId).DisplayName;
        //            }
        //            catch (GraphException eprime)
        //            {
        //                if (eprime.HttpStatusCode == HttpStatusCode.NotFound)
        //                {
        //                    try
        //                    {
        //                        // If the User and Group with ObjectID, Get a Built-In Directory Role
        //                        return graphConnection.Get<Role>(objectId).DisplayName;
        //                    }
        //                    catch (GraphException eprimeprime)
        //                    {
        //                        if (eprimeprime.HttpStatusCode == HttpStatusCode.NotFound)
        //                        {
        //                            // If neither a User nor a Group nor a Role was found, return null
        //                            return null;
        //                        }
        //                        else { throw eprimeprime; }
        //                    }
        //                }
        //                else { throw eprime; }
        //            }
        //        }
        //        else { throw e; }
        //    }
        //}
        //#endregion

        private async Task<List<KeyValuePair<string, string>>> GetApplicationRoles()
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            // Get Access Token for App's Directory
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.AppAuthority, new TokenDbCache(ConfigHelper.ClientId));
            AuthenticationResult authResult = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, ConfigHelper.ClientId);

            // Query the Graph for all Application Roles
            string requestUri = String.Format(CultureInfo.InvariantCulture, 
                "{0}{1}/applications?$filter=appId eq '{2}'&api-version={3}",
                ConfigHelper.GraphResourceId, 
                ConfigHelper.AppTenant, 
                ConfigHelper.ClientId, 
                ConfigHelper.GraphApiVersion);
            string serialResponse = SendGetAsync(requestUri, authResult.AccessToken);
            AppQueryResult apps = JsonConvert.DeserializeObjectAsync<AppQueryResult>(serialResponse);
            foreach (AppRole appRole in apps.Value[0].appRoles)
                result.Add(new KeyValuePair<string, string>(appRole.Id, appRole.displayName));

            return result;
        }

        private async UserQueryResult GetAllUsers()
        {
            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
            ClientCredential cred = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
            AuthenticationResult authResult = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, ConfigHelper.ClientId, new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

            string requestUri = String.Format(CultureInfo.InvariantCulture,
                "{0}{1}/users?api-version={2}",
                ConfigHelper.GraphResourceId,
                ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value,
                ConfigHelper.GraphApiVersion);

            string serialResponse = await SendGetAsync(requestUri, authResult.AccessToken);
            return JsonConvert.DeserializeObject<UserQueryResult>(serialResponse);
        }

        private async GroupQueryResult GetAllGroups()
        {
            string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
            ClientCredential cred = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectId));
            AuthenticationResult authResult = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, ConfigHelper.ClientId, new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

            string requestUri = String.Format(CultureInfo.InvariantCulture,
                "{0}{1}/groups?api-version={2}",
                ConfigHelper.GraphResourceId,
                ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value,
                ConfigHelper.GraphApiVersion);

            string serialResponse = await SendGetAsync(requestUri, authResult.AccessToken);
            return JsonConvert.DeserializeObject<GroupQueryResult>(serialResponse);
        }

        private async string SendGetAsync(string requestUri, string accessToken)
        {
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> contentType = new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>();
            contentType.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept = contentType;
            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new WebException();

            return await response.Content.ReadAsStringAsync();
        }

        private abstract class IUserGroup
        { 
            // Common properties that we'll actually use.
            // displayName, objectId
        }

        private class AppQueryResult
        { 
            // Need JSON response for this.
        }

        private class UserQueryResult
        { 
            // Need JSON response for this.
        }

        private class GroupQueryResult
        {
            // Need JSON response for this.
        }

        private class Value : IUserGroup
        { 
            // This will be generated by Json2C#
        }
    
    }
}