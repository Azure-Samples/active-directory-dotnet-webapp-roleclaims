using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using WebApp_RoleClaims_DotNet.Models;

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
            Dictionary<string, List<Assignment>> roleAssignments = new Dictionary<string,List<Assignment>>();
            List<KeyValuePair<string, string>> appRoleGuids = null;

            // Get All Roles for the Application
            try
            {
                appRoleGuids = await GetApplicationRoles();
                foreach (KeyValuePair<string, string> kvp in appRoleGuids)
                    roleAssignments[kvp.Key] = new List<Assignment>();


            }
            catch (AuthenticationException e)
            {
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting application roles." });
            }
            catch (AdalException e)
            {
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting application roles." });
            }

            // Get All App Role Assignments
            try
            {
                roleAssignments = await GetAppRoleAssignments();
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

            string accessToken = await GraphHelper.AcquireToken();

            ViewData["roleGuids"] = appRoleGuids;
            ViewData["roleAssignments"] = roleAssignments;
            ViewData["token"] = accessToken;
            ViewData["tenant"] = ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value;
            return View();
        }


        /// <summary>
        /// Adds a User/Group<-->Application Role mapping from user input form
        /// to roles.xml if it does not already exist.
        /// </summary>
        /// <param name="formCollection">The user input form, containing the UPN or GroupName
        /// of the object to grant a role.</param>
        /// <returns>A Redirect to the Roles page.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignRole(FormCollection formCollection)
        {
            // Check for an input
            if (formCollection != null && formCollection["id"].Length > 0)
            {
                //Get the Access Token for Calling Graph API from the cache
                AuthenticationResult result = null;
                try
                {
                    ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => await GraphHelper.AcquireToken());
                    IPagedCollection<IDirectoryObject> dirObjects = await graphClient.DirectoryObjects.Where(o => o.ObjectId.Equals(formCollection["id"])).ExecuteAsync();
                    DirectoryObject dirObject = (DirectoryObject) dirObjects.CurrentPage[0];

                    IPagedCollection<IServicePrincipal> servicePrincipals = await graphClient.ServicePrincipals.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
                    ServicePrincipal sp = (ServicePrincipal)servicePrincipals.CurrentPage[0];
                    
                    AppRoleAssignment newAssignment = new AppRoleAssignment();
                    newAssignment.Id = Guid.Parse(formCollection["role"]);
                    newAssignment.ResourceId = Guid.Parse(sp.ObjectId);
                    newAssignment.PrincipalType = dirObject.ObjectType;
                    newAssignment.PrincipalId = Guid.Parse(dirObject.ObjectId);

                    if(dirObject is User || dirObject is Group)
                    {
                        dynamic userGroup = dirObject;
                        userGroup.AppRoleAssignments.Add(newAssignment);
                        await userGroup.UpdateAsync();
                    }
                }
                catch (Exception e)
                {
                    return RedirectToAction("ShowError", "Error", new { errorMessage = "Error While Adding Role Assignment." });                
                }
            }

            return RedirectToAction("Index", "Roles", null);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RemoveRole(FormCollection formCollection)
        {
            List<Task<HttpResponseMessage>> requests = new List<Task<HttpResponseMessage>>();
            string accessToken = await GraphHelper.AcquireToken();
            // Remove role mapping assignments marked by checkboxes
            try 
            {
                foreach (string key in formCollection.Keys)
                {
                    if (formCollection[key].Equals("delete"))
                    {
                        string[] temp = key.Split(' ');
                        string assignmentId = temp[0];
                        string principalId = temp[1];
                        string principalType = temp[2].ToLower() + 's';

                        string requestUri = String.Format(CultureInfo.InvariantCulture,
                            "{0}{1}/{2}/{3}/appRoleAssignments/{4}?api-version={5}",
                            ConfigHelper.GraphResourceId,
                            ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value,
                            principalType,
                            principalId,
                            assignmentId,
                            ConfigHelper.GraphApiVersion);

                        HttpClient client = new HttpClient();
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        requests.Add(client.SendAsync(request));
                    }
                }
                HttpResponseMessage[] responses = await System.Threading.Tasks.Task.WhenAll(requests);
                foreach (HttpResponseMessage resp in responses) {
                    if (!resp.IsSuccessStatusCode)
                        throw new WebException();
                }
            }
            catch (Exception e)
            {
                 return RedirectToAction("ShowError", "Error", new { errorMessage = "Error While Deleting Role Assignment." });
            }

            return RedirectToAction("Index", "Roles", null);
        }

        /// <summary>
        /// Used for the AadPickerLibrary that is used to search for users and groups.  Accepts a user input
        /// and a number of results to retreive, and queries the graphAPI for possbble matches.
        /// </summary>
        /// <returns>JSON containing query results ot the Javascript library.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async System.Threading.Tasks.Task<ActionResult> Search(string query, string token)
        {
            // Search for users based on user input.
            try
            {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, query);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return this.Content(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    return Json(new { error = "graph api error" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { error = "internal server error" }, JsonRequestBehavior.AllowGet);
            }
        }

        private async Task<List<KeyValuePair<string, string>>> GetApplicationRoles()
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => await GraphHelper.AcquireToken());
            IPagedCollection<IServicePrincipal> servicePrincipals = await graphClient.ServicePrincipals.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
            ServicePrincipal sp = (ServicePrincipal)servicePrincipals.CurrentPage[0];

            foreach (AppRole appRole in sp.AppRoles)
                result.Add(new KeyValuePair<string, string>(appRole.Id.ToString(), appRole.DisplayName));

            return result;
        }

        private async Task<Dictionary<string, List<Assignment>>> GetAppRoleAssignments()
        {
            Dictionary<string, List<Assignment>> results = new Dictionary<string, List<Assignment>>();

            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => await GraphHelper.AcquireToken());
            IPagedCollection<IServicePrincipal> servicePrincipals = await graphClient.ServicePrincipals.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
            ServicePrincipal servicePrincipal = (ServicePrincipal)servicePrincipals.CurrentPage[0];

            string requestUri = String.Format(CultureInfo.InvariantCulture,
                "{0}{1}/servicePrincipals/{2}/appRoleAssignments?api-version={3}",
                ConfigHelper.GraphResourceId,
                ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value,
                servicePrincipal.ObjectId,
                ConfigHelper.GraphApiVersion);

            string serialResponse = await SendGetAsync(requestUri);
            JObject jResult = JObject.Parse(serialResponse);

            foreach (JObject appRoleAssignment in jResult["value"])
            {
                List<Assignment> tempList = null;
                results.TryGetValue((string)appRoleAssignment["id"], out tempList);
                if (tempList == null)
                    results[(string)appRoleAssignment["id"]] = new List<Assignment>();
                results[(string)appRoleAssignment["id"]].Add(new Assignment
                {
                    displayName = (string)appRoleAssignment["principalDisplayName"],
                    objectType = (string)appRoleAssignment["principalType"],
                    objectId = (string)appRoleAssignment["principalId"],
                    assignmentId = (string)appRoleAssignment["objectId"]
                });
            }

            return results;
        }

        private async Task<string> SendGetAsync(string requestUri)
        {
            string accessToken = await GraphHelper.AcquireToken();

            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new WebException();

            return await response.Content.ReadAsStringAsync();
        }

        //private async Task UpdateRoleAssignments(T )
    }
}