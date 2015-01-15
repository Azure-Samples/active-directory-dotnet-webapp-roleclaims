WebApp-RoleClaims-DotNet
========================

This sample shows how to build an MVC web application that uses Azure AD Application Roles for authorization. Authorization in Azure AD can also be done with Azure AD Groups, as shown in [WebApp-GroupClaims-DotNet](https://github.com/AzureADSamples/WebApp-GroupClaims-DotNet). This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

##About The Sample

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, access to certain functionality is restricted to subsets of users. For instance, not every user has the ability to create a task.

This kind of authorization is implemented using role based access control (RBAC).  When using RBAC, an administrator grants permissions to roles, not to individual users or groups. The administrator can then assign roles to different users and groups to control who has access to what content and functionality.  

This application implements RBAC using Azure AD's Application Roles & Role Claims features.  Another approach is to use Azure AD Groups and Group Claims, as shown in [WebApp-GroupClaims-DotNet](https://github.com/AzureADSamples/WebApp-GroupClaims-DotNet).  Azure AD Groups and Application Roles are by no means mutually exclusive - they can be used in tandem to provide even finer grained access control.

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
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [http://wwww.windowsazure.com](http://www.windowsazure.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/AzureADSamples/WebApp-RoleClaims-DotNet.git`

### Step 2: Run the Sample

This sample is already registered in a Microsoft tenant as a multi-tenant application that can run out of the box with your tenant by following these steps:

1. Run the app in Visual Studio and sign in as a user in your AAD tenant, granting consent when prompted to do so.
2. In the [Azure management portal](https://manage.windowsazure.com), navigate to your tenant by clicking on Active Directory in the left hand nav and selecting the appropriate tenant.
3. Click the "Applications" tab, and locate the newly created entry for "WebApp-RoleClaims-DotNet." Click on it.
4. On the following page, click on the "Users" tab.  Select any user, click the "Assign" button in the bottom tray, and assign the user to an Application Role.  Repeat this process for any users you would like to have access to Tasks in the application.
5. Sign out of the sample application and sign back in.

Explore the application by assigning various users and groups to roles via Azure Portal. Login as users in different roles, and notice the differences in functionality available to each.  Each role has different capabilities on the "Tasks" page, as described above.

## How To Run The Sample as a Single Tenant App

This section explains how to register the application as a single tenant application in your own tenant, rather than in a Microsoft tenant. 

### Step 1:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "TaskTrackerWebApp", select "Web Application and/or Web API", and click next.
8. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44322/`.  NOTE:  It is important, due to the way Azure AD matches URLs, to ensure there is a trailing slash on the end of this URL.  If you don't include the trailing slash, you will receive an error when the application attempts to redeem an authorization code.
9. For the App ID URI, enter `https://<your_tenant_name>/<your_application_name>`, replacing `<your_tenant_name>` with the name of your Azure AD tenant and `<your_application_name>` with the name you chose above.  Click OK to complete the registration.
10. While still in the Azure portal, click the Configure tab of your application.
11. Find the Client ID value and copy it aside, you will need this later when configuring your application.
12. Create a new key for the application.  Save the configuration so you can view the key value.  Save this key aside, you'll need it shortly as well.
13. In the Permissions to Other Applications configuration section, ensure that both "Access your organization's directory" and "Enable sign-on and read user's profiles" are selected under "Delegated permissions" for "Windows Azure Active Directory"  Save the configuration.

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
    <<<< Approver, Observer, & Writer roles go here >>>>
  ],
```

### Step 3:  Configure the sample to use your Azure AD tenant

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
4. Find the app key `ida:ClientId` and replace the value with the Client ID for the application from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for the application from the Azure portal.
6. Find the app key `ida:Tenant` and replace the value with the domain of your tenant.
6. If you changed the base URL of the TodoListWebApp sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.
7. In `Startup.Auth.cs`, comment out or delete the lines corresponding to the multi-tenant version of the sample, which are marked by comments.  You'll have to change the value for the `Authority` to the single-tenant version, and delete the lines relating to `TokenValidationParameters`.

### Step 4:  Run the sample

Clean the solution, rebuild the solution, and run it!  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and assign them different roles by navigating to the "Users" tab of your application in the Azure Portal.  Create a Security Group in the Azure Management Portal, add users to it, and again add roles to it using an Admin account.  Explore the differences between each role throughout the application, namely the Tasks page.

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Web Sites in the left hand nav.
3. Click New in the bottom left hand corner, select Compute --> Web Site --> Quick Create, select the hosting plan and region, and give your web site a name, e.g. tasktracker-contoso.azurewebsites.net.  Click Create Web Site.
4. Once the web site is created, click on it to manage it.  For the purposes of this sample, download the publish profile from Quick Start or from the Dashboard and save it.  Other deployment mechanisms, such as from source control, can also be used.
5. While still in the Azure management portal, navigate back to the Azure AD tenant you used in creating this sample.  Under applications, select your Task Tracker application.  Under configure, update the Sign-On URL and Reply URL fields to the root address of your published application, for example https://tasktracker-contoso.azurewebsites.net/.  Click Save.
5. Switch to Visual Studio and go to the WebApp-RoleClaims-DotNet project.  In the web.config file, update the "PostLogoutRedirectUri" value to the root address of your published appliction as well.
6. Right click on the project in the Solution Explorer and select Publish.  Under Profile, click Import, and import the publish profile that you just downloaded.
6. On the Connection tab, update the Destination URL so that it is https, for example https://tasktracker-contoso.azurewebsites.net.  Click Next.
7. On the Settings tab, make sure Enable Organizational Authentication is NOT selected.  Click Publish.
8. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

## Code Walk-Through

Coming soon.
