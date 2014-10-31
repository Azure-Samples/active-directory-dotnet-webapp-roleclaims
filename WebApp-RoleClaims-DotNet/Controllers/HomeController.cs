using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

//The following libraries were added to this sample.
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebApp_RoleClaims_DotNet.Utils;
using System.Security.Claims;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Net;
using System.Linq.Expressions;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System.Threading.Tasks;


//The following libraries were defined and added to this sample.



namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Shows the generic MVC Get Started Home Page. Allows unauthenticated
        /// users to see the home page and click the sign-in link.
        /// </summary>
        /// <returns>Generic Home <see cref="View"/>.</returns>
        [Authorize]
        public async Task<ActionResult> Index()
        {
            ActiveDirectoryClient activeDirectoryClient;
            // Setup the graph connection
            try
            {
                activeDirectoryClient = new ActiveDirectoryClient(ConfigHelper.GraphServiceRoot, async () => await GraphHelper.AcquireTokenForGraph());
            }
            catch (AuthenticationException e)
            { 
                // If the user doesn't have an access token, they need to re-authorize
                if (e.Code == "failed_to_acquire_token_silently")
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });

                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while acquiring token." });
            }

            
            IPagedCollection<IApplication> pagedApps = null;
            try
            {
                IPagedCollection<IUser> users = await activeDirectoryClient.Users.Where(user => user.ObjectId.Equals(ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value)).ExecuteAsync();
                IUserFetcher userFetcher = (IUserFetcher)users.CurrentPage[0];
                AppRoleAssignment temp2 = new AppRoleAssignment();
                IAppRoleAssignmentFetcher temp = (IAppRoleAssignmentFetcher)users.CurrentPage[0].AppRoleAssignments.CurrentPage[0];
                IAppRoleAssignmentFetcher userRoleAssignments = userFetcher.ToAppRoleAssignment();
                //pagedApps = await activeDirectoryClient.Applications.Where(app => app.AppId.Equals(ConfigHelper.ClientId)).ExecuteAsync();
                //pagedApps = activeDirectoryClient.Applications.Take(999).ExecuteAsync().Result;

            }
            catch (Exception e) 
            { 
                
            }

            return View();
        }

        /// <summary>
        /// Gets user specific RBAC information: The Security Groups the user belongs to
        /// And the application roles the user has been granted.
        /// </summary>
        /// <returns>The About <see cref="View"/>.</returns>
        [Authorize]
        public ActionResult About()
        {
            var myroles = new List<String>();
            var mygroups = new List<String>();

            AuthenticationContext authContext;

            foreach (Claim claim in ClaimsPrincipal.Current.FindAll("Role")) //TODO: Is this right?
                myroles.Add(claim.Value);
            
            //Get the Access Token for Calling Graph API frome the cache
            AuthenticationResult result = null;
            try
            {
                string userObjectId = ClaimsPrincipal.Current.FindFirst(Globals.ObjectIdClaimType).Value;
                authContext = new AuthenticationContext(ConfigHelper.Authority,
                    new TokenDbCache(userObjectId));
                var credential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                result = authContext.AcquireTokenSilent(ConfigHelper.GraphResourceId, credential,
                    new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
            }
            catch (AdalException e)
            {
                // If the user doesn't have an access token, they need to re-authorize
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                        return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });

                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while acquiring token." });
            }

            // Setup Graph Connection
            //Guid clientRequestId = Guid.NewGuid();
            var graphSettings = new GraphSettings();
            graphSettings.ApiVersion = ConfigHelper.GraphApiVersion; //TODO
            //var graphConnection = new GraphConnection(result.AccessToken, clientRequestId, graphSettings);
            GraphConnection graphConnection = new GraphConnection(result.AccessToken, graphSettings);
            Dictionary<string, string> groupNameDict = new Dictionary<string, string>();

            try { 
                // For each Group Claim, we need to get the DisplayName of the Group from the GraphAPI
                // We choose to iterate over the set of all groups rather than batch query the GraphAPI for each group.
                // First, put all <GroupObjectID, DisplayName> pairs into a dictionary

                PagedResults<Group> pagedResults = null;
                do
                {
                    string pageToken = pagedResults == null ? null : pagedResults.PageToken;
                    pagedResults = graphConnection.List<Group>(pageToken, null);
                    foreach (Group group in pagedResults.Results)
                        groupNameDict[group.ObjectId] = group.DisplayName;
                } while (!pagedResults.IsLastPage);
            }
            catch (GraphException e) {

                if (e.HttpStatusCode == HttpStatusCode.Unauthorized) {
                    // The user needs to re-authorize.  Show them a message to that effect.
                    authContext.TokenCache.Clear();
                    return RedirectToAction("Reauth", "Error", new { redirectUri = Request.Url });
                }

                return RedirectToAction("ShowError", "Error", new { errorMessage = "Error while calling Graph API." });
            }

            // For the security groups the user is a member of, get the DisplayName
            foreach (Claim claim in ClaimsPrincipal.Current.FindAll("groups"))
            {
                if (claim.Value == "error")
                    mygroups.Add("Error getting groups from Graph API.");
                string displayName;
                if (groupNameDict.TryGetValue(claim.Value, out displayName)) {
                    mygroups.Add(displayName);
                }
            }

            ViewData["myroles"] = myroles;
            ViewData["mygroups"] = mygroups;
            return View();
        }
    }
}