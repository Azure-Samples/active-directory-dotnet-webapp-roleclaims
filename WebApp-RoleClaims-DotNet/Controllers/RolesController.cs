using System;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

//The following libraries were defined and added to this sample.
using WebApp_RoleClaims_DotNet.Utils;
using WebApp_RoleClaims_DotNet.Models;


namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class RolesController : Controller
    {
        #region controller_actions

        // Show the Current Role Assignments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Index()
        {
            Dictionary<string, List<Assignment>> roleAssignments = new Dictionary<string,List<Assignment>>();
            List<KeyValuePair<string, string>> appRoleGuids = null;
            string accessToken = null;

            // Get All Roles for the Application, So The List is Always Up-To-Date
            try
            {
                appRoleGuids = await GetApplicationRoles();
                foreach (KeyValuePair<string, string> kvp in appRoleGuids)
                    roleAssignments[kvp.Key] = new List<Assignment>();
                accessToken = SilentTokenHelper.AcquireToken();

            }
            catch (AdalException e)
            {
                // If So, User May Need to Re-Authenticate
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting application roles." });
            }
            catch (Exception)
            {
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting application roles." });
            }

            // Get All App Role Assignments For the Service Principal
            try
            {
                await GetAppRoleAssignments(roleAssignments);
            }
            catch (AdalException e)
            {
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting role assignments." });
            }
            catch (Exception)
            { 
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while getting role assignments." });                
            }

            // Need the Role Id <--> Role Display Name Pairs, Since Graph Only Returns Role Id's on appRoleAssignments
            ViewData["roleGuids"] = appRoleGuids;
            // The Dictionary of Users and Groups Assigned to Each Role
            ViewData["roleAssignments"] = roleAssignments;
            // An Access Token for the People Picker Javascript to Use
            ViewData["token"] = accessToken;
            // The Tenant for the People Picker Javascript to Use
            ViewData["tenant"] = ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value;
            return View();
        }


        // Add a new Role Assignment via the Graph API
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignRole(FormCollection formCollection)
        {
            // Check for an input objectId
            if (formCollection != null && formCollection["id"].Length > 0)
            {
                try
                {
                    // Get the User or Group By ObjectId (using Graph Client Library)
                    ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => { return SilentTokenHelper.AcquireToken(); });
                    IPagedCollection<IDirectoryObject> dirObjects = await graphClient.DirectoryObjects.Where(o => o.ObjectId.Equals(formCollection["id"])).ExecuteAsync();
                    DirectoryObject dirObject = (DirectoryObject) dirObjects.CurrentPage[0];

                    // Get the Service Principal by AppId (using Graph Client Library)
                    IPagedCollection<IServicePrincipal> servicePrincipals = await graphClient.ServicePrincipals.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
                    ServicePrincipal sp = (ServicePrincipal)servicePrincipals.CurrentPage[0];
                    
                    // Create a new AppRoleAssignment
                    AppRoleAssignment newAssignment = new AppRoleAssignment
                    {
                        Id = Guid.Parse(formCollection["role"]),
                        ResourceId = Guid.Parse(sp.ObjectId),
                        ObjectType = dirObject.ObjectType,
                        PrincipalId = Guid.Parse(dirObject.ObjectId)
                    };
                    
                    // Add the AppRoleAssignment (using Graph Client Library)
                    if(dirObject is User || dirObject is Group)
                    {
                        dynamic userGroup = dirObject;
                        userGroup.AppRoleAssignments.Add(newAssignment);
                        await userGroup.UpdateAsync();
                    }
                }
                catch (AdalException e)
                {
                    if (e.ErrorCode == "failed_to_acquire_token_silently")
                        return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                    return RedirectToAction("ShowError", "Error", new { errorMessage = "Error While Adding Role Assignment." });
                }
                catch (Exception)
                {
                    return RedirectToAction("ShowError", "Error", new { errorMessage = "Error While Adding Role Assignment." });                
                }
            }

            return RedirectToAction("Index", "Roles", null);
        }

        // Remove Users or Groups from Roles
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RemoveRole(FormCollection formCollection)
        {
            List<Task<HttpResponseMessage>> requests = new List<Task<HttpResponseMessage>>();
            try
            {
                string accessToken = SilentTokenHelper.AcquireToken();
                foreach (string key in formCollection.Keys) {
                    // If the checkbox is selected, delete the AppRoleAssignment (using REST calls)
                    if (formCollection[key].Equals("delete"))
                    {
                        string[] values = key.Split(' ');

                        string requestUri = String.Format(CultureInfo.InvariantCulture,
                            "{0}{1}/{2}/{3}/appRoleAssignments/{4}?api-version={5}",
                            ConfigHelper.GraphResourceId,
                            ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value,
                            values[2].ToLower() + 's',  // 'groups' or 'users'
                            values[1],                  // principalId
                            values[0],                  // assignmentId
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
            catch (AdalException e)
            {
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error While Adding Role Assignment." });
            }
            catch (Exception e)
            {
                 return RedirectToAction("ShowError", "Error", new { errorMessage = "Error While Deleting Role Assignment." });
            }

            return RedirectToAction("Index", "Roles", null);
        }

        // Used By People Picker Library to Make Graph Calls (CORS not supported)
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
            catch (Exception)
            {
                return Json(new { error = "internal server error" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region helper_methods

        // Get All Application Roles for this App.
        private async Task<List<KeyValuePair<string, string>>> GetApplicationRoles()
        {
            // Query the Graph for the ServicePrincipal (using the Graph Client Library)
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => { return SilentTokenHelper.AcquireToken(); });
            IPagedCollection<IServicePrincipal> servicePrincipals = await graphClient.ServicePrincipals.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
            ServicePrincipal sp = (ServicePrincipal)servicePrincipals.CurrentPage[0];

            foreach (AppRole appRole in sp.AppRoles)
                result.Add(new KeyValuePair<string, string>(appRole.Id.ToString(), appRole.DisplayName));

            return result;
        }

        // Get All App Role Assignments for this Service Principal.
        private async Task<Dictionary<string, List<Assignment>>> GetAppRoleAssignments(Dictionary<string, List<Assignment>> results)
        {
            // Query the Graph for this Service Principal (using Graph Client Library)
            ActiveDirectoryClient graphClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => { return SilentTokenHelper.AcquireToken(); });
            IPagedCollection<IServicePrincipal> servicePrincipals = await graphClient.ServicePrincipals.Where(a => a.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
            ServicePrincipal servicePrincipal = (ServicePrincipal)servicePrincipals.CurrentPage[0];

            // Query the Graph for All AppRoleAssignment objects (using REST calls)
            string requestUri = String.Format(CultureInfo.InvariantCulture,
                "{0}{1}/servicePrincipals/{2}/appRoleAssignments?api-version={3}",
                ConfigHelper.GraphResourceId,
                ClaimsPrincipal.Current.FindFirst(Globals.TenantIdClaimType).Value,
                servicePrincipal.ObjectId,
                ConfigHelper.GraphApiVersion);

            // Send the Query
            string accessToken = SilentTokenHelper.AcquireToken();
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new WebException();
            string serialResponse = await response.Content.ReadAsStringAsync();
            JObject jResult = JObject.Parse(serialResponse);

            // Extract Necessary Information From Weakly Typed JSON
            foreach (JObject appRoleAssignment in jResult["value"])
            {
                List<Assignment> tempList = null;
                if(results.TryGetValue((string)appRoleAssignment["id"], out tempList))
                {
                    results[(string)appRoleAssignment["id"]].Add(new Assignment
                    {
                        displayName = (string)appRoleAssignment["principalDisplayName"],
                        objectType = (string)appRoleAssignment["principalType"],
                        objectId = (string)appRoleAssignment["principalId"],
                        assignmentId = (string)appRoleAssignment["objectId"]
                    });
                }
            }

            return results;
        }

        #endregion
    }
}