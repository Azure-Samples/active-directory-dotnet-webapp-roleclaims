---
services: active-directory
platforms: dotnet
author: dstrockis
---

# Authorization in a web app using Azure AD application roles & role claims


This sample shows how to build an MVC web application that uses Azure AD Application Roles for authorization. Authorization in Azure AD can also be done with Azure AD Groups, as shown in [WebApp-GroupClaims-DotNet](https://github.com/Azure-Samples/WebApp-GroupClaims-DotNet). This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

##About The Sample

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, access to certain functionality is restricted to subsets of users. For instance, not every user has the ability to create a task.

This kind of authorization is implemented using role based access control (RBAC).  When using RBAC, an administrator grants permissions to roles, not to individual users or groups. The administrator can then assign roles to different users and groups to control who has access to what content and functionality.  

This application implements RBAC using Azure AD's Application Roles & Role Claims features.  Another approach is to use Azure AD Groups and Group Claims, as shown in [WebApp-GroupClaims-DotNet](https://github.com/Azure-Samples/WebApp-GroupClaims-DotNet).  Azure AD Groups and Application Roles are by no means mutually exclusive - they can be used in tandem to provide even finer grained access control.

Our Task Tracker application defines four *Application Roles*:
- Admin: Has the ability to perform all actions, as well as manage the Application Roles.
- Writer: Has the ability to create tasks.
- Approver: Has the ability to change the status of tasks.
- Observer: Only has the ability to view tasks and their statuses.

These application roles are defined in the [Azure Management Portal](https://manage.windowsazure.com/) on the application registration page.  When a user signs into the application, AAD emits a Role Claim for each role that has been granted based on the user and their group membership.  Assignment of users and groups to roles can be done through the portal's UI, or programatically using the [AAD Graph API](http://msdn.microsoft.com/en-us/library/azure/hh974476.aspx).  In this sample, application role management is done through the Azure Portal.

Using RBAC with Application Roles and Role Claims, this application securely enforces authorization policies with minimal effort on the part of the developer.

NOTE: Role claims are not currently emitted in SAML tokens, only JWTs (see issue #1).

NOTE: Role claims are not currently emitted for guest users in a tenant (see issue #2).

## How To Run The Sample as a MultiTenant App

To run this sample you will need:
- Visual Studio 2013
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, please see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/) 
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account, so if you signed in to the Azure portal with a Microsoft account and have never created a user account in your directory before, you need to do that now.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-authz-roleclaims.git`

### Step 2: Run the Sample

This sample is already registered in a Microsoft tenant as a multi-tenant application that can run out of the box with your tenant by following these steps:

1. Run the app in Visual Studio and sign in as a user in your AAD tenant, granting consent when prompted to do so.  NOTE: you can't use an MSA guest user account to sign in - it must be a user that you created in your tenant.
2. In the [Azure management portal](https://manage.windowsazure.com), navigate to your tenant by clicking on Active Directory in the left hand nav and selecting the appropriate tenant.
3. Click the "Applications" tab, and locate the newly created entry for "WebApp-RoleClaims-DotNet." Click on it.
4. On the following page, click on the "Users" tab.  Select any user, click the "Assign" button in the bottom tray, and assign the user to an Application Role.  Repeat this process for any users you would like to have access to Tasks in the application.
5. Sign out of the sample application and sign back in.

Explore the application by assigning various users and groups to roles via Azure Portal. Login as users in different roles, and notice the differences in functionality available to each.  Each role has different capabilities on the "Tasks" page, as described above.

## How To Run The Sample as a Single Tenant App

This section explains how to register the application as a single tenant application in your own tenant, rather than in a Microsoft tenant. 

### Step 1:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure portal](https://portal.azure.com).
2. On the top bar, click on your account and under the **Directory** list, choose the Active Directory tenant where you wish to register your application.
3. Click on **More Services** in the left hand nav, and choose **Azure Active Directory**.
4. Click on **App registrations** and choose **Add**.
5. Enter a friendly name for the application, for example 'TaskTrackerWebApp' and select 'Web Application and/or Web API' as the Application Type. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44322/`. NOTE:  It is important, due to the way Azure AD matches URLs, to ensure there is a trailing slash on the end of this URL.  If you don't include the trailing slash, you will receive an error when the application attempts to redeem an authorization code. For the App ID URI, enter `https://<your_tenant_name>/<your_application_name>`, replacing `<your_tenant_name>` with the name of your Azure AD tenant and `<your_application_name>` with the name you chose above. Click on **Create** to create the application.
6. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
7. Find the Application ID value and copy it to the clipboard.
8. On the same page, change the `Logout Url` field to `https://localhost:44322/Account/EndSession`.  This is the default single sign out URL for this sample.
9. From the Settings menu, choose **Keys** and add a key - select a key duration of either 1 year or 2 years. When you save this page, the key value will be displayed, copy and save the value in a safe location - you will need this key later to configure the project in Visual Studio - this key value will not be displayed again, nor retrievable by any other means, so please record it as soon as it is visible from the Azure Portal.
10. Configure Permissions for your application - in the Settings menu, choose the 'Required permissions' section, click on **Add**, then **Select an API**, and select 'Microsoft Graph' (this is the Graph API). Then, click on  **Select Permissions** and select 'Read Directory Data' and 'Sign in and read user profile'.

### Step 2: Define your Application Roles

1. While still in the Configure tab of your application, click "Manage Manifest" in the drawer, and download the existing manifest.
2. Edit the downloaded manifest by locating the "appRoles" setting and adding all four Application Roles.  The Admin role definition is provided in the JSON block below.  Leave the allowedMemberTypes to "User" only.  Each role definition in this manifest must have a different valid Guid for the "id" property.  Define each of the four "value" properties with the exact strings "Admin", "Approver", "Observer", and "Writer".
3. Save and upload the edited manifest using the same "Manage Manifest" button in the portal.
```JSON
"appRoles": [
    {
        "allowedMemberTypes": [
            "User"
        ],
        "description": "Admins can manage roles and perform all task actions.",
        "displayName": "Admin",
        "id": "81e10148-16a8-432a-b86d-ef620c3e48ef",
        "isEnabled": true,
        "origin": "Application",
        "value": "Admin"
    },
    {
        "allowedMemberTypes": [
            "User"
        ],
        "description": "Approvers can change the status of an existing task, but cannot add a new task.",
        "displayName": "Approver",
        "id": "86ea7495-44bd-4f23-8ee1-b2fdc7bb0735",
        "isEnabled": true,
        "origin": "Application",
        "value": "Approver"
    },
    {
        "allowedMemberTypes": [
            "User"
        ],
        "description": "Observers can only read the tasks and their statuses.",
        "displayName": "Observer",
        "id": "42d3e6fb-2255-4fc9-97ec-7cf2dc8117e9",
        "isEnabled": true,
        "origin": "Application",
        "value": "Observer"
    },
    {
        "allowedMemberTypes": [
            "User"
        ],
        "description": "Writers can add new tasks, but cannot change the status of an existing task.",
        "displayName": "Writer",
        "id": "c4b84e95-2af9-45f9-8da1-11c597079e0d",
        "isEnabled": true,
        "origin": "Application",
        "value": "Writer"
    }
],
```

### Step 3:  Configure the sample to use your Azure AD tenant

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
4. Find the app key `ida:ClientId` and replace the value with the Application ID for the application from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for the application from the Azure portal.
6. Find the app key `ida:Tenant` and replace the value with the domain of your tenant.
6. If you changed the base URL of the TodoListWebApp sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.
7. In `Startup.Auth.cs`, comment out or delete the lines corresponding to the multi-tenant version of the sample, which are marked by comments.  You'll have to change the value for the `Authority` to the single-tenant version, and delete the line relating to `ValidateIssuer` in `TokenValidationParameters`.

### Step 4:  Run the sample

Clean the solution, rebuild the solution, and run it!  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and assign them different roles by navigating to the "Users" tab of your application in the Azure Portal.  Create a Security Group in the Azure Management Portal, add users to it, and again add roles to it using an Admin account.  Explore the differences between each role throughout the application, namely the Tasks page.

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure portal](https://portal.azure.com).
2. Click New in the top left hand corner, select Web + Mobile --> Web App, select the hosting plan and region, and give your web site a name, e.g. todolistservice-contoso.azurewebsites.net.  Click Create Web Site.
3. Once the web site is created, click on it to manage it.  For this set of steps, download the publish profile and save it.  Other deployment mechanisms, such as from source control, can also be used.
4. While still in the Azure portal, navigate back to the Azure AD tenant you used in creating this sample.  Under applications, select your Task Tracker application.  From the **Settings** page, update the Sign-On URL and Reply URL fields to the root address of your published application, for example https://tasktracker-contoso.azurewebsites.net/. 
5. Switch to Visual Studio and go to the WebApp-RoleClaims-DotNet project. In the web.config file, update the "PostLogoutRedirectUri" value to the root address of your published application as well.
6. Right click on the project in the Solution Explorer and select Publish.  Click Import, and import the publish profile that you just downloaded.
7. On the Connection tab, update the Destination URL so that it is https, for example https://tasktracker-contoso.azurewebsites.net.  Click Next.
8. On the Settings tab, make sure Enable Organizational Authentication is NOT selected.  Click Publish.
9. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

## Code Walk-Through

Coming soon.
