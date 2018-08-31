# Using Azure AD application roles & role claims in a multi-tenant application

As this sample application is already registered in a Microsoft tenant as a multi-tenant application, you can run out of the box with your tenant by following these steps:

To achieve this requires a small change to the application itself, but there are contraints on the type of user.

## Prerequisites
To run this sample as a multi-tenant app, the following extra prerequistes apply as well.
- One or more user accounts in your Azure AD tenant. In the first case (run as a multi-tenant application), this sample will **not** work with **a Microsoft Personal account**.

## Step 1: How to run the sample as a Multi-Tenant App
1. In Visual Studio, find the tenant key `ida:Tenant` (in the `Web.Config` file) and replace the value with the domain of your tenant. Do not change the other keys.
1. Open up **Startup.cs** in Visual Studio.Net and change the code as following to switch the app to a multi-tenant app in code.
```C#
        public void Configuration(IAppBuilder app)
        {
            // Comment the following line to try out the multi-tenant scenario
            // ConfigureAuth(app);

            // Uncomment the following line to try out the multi-tenant scenario
             ConfigureMultitenantAuth(app);
        }
```
1. Run the app in Visual Studio and sign in as a user in your AAD tenant, granting consent when prompted to do so.  
    - NOTE: you can't use an MSA guest user account to sign in - it must be a user that you created in your tenant as explained above.
    - At that point, once you have granted consent, if you go to the "Tasks" menu command, you will see "You do not have sufficient privileges to view this page". This is normal, because we have not assigned any role to users in your directory yet. That's the goal of  next steps (3 to 5).
1. In the [Azure portal](https://portal.azure.com), navigate to your tenant by clicking on Active Directory in the left hand nav and selecting the appropriate tenant. Note that if you choose to use the
[Azure portal](https://portal.azure.com) instead, you would find the provisioned application under *Enterprise applications* and you would have to adapt these steps slightly.
1. Navigate back to **Azure Active Directory** pane, and click on *Enterprise applications*.
1. Click the "All Applications" tab, and locate the newly created entry for "WebApp-RoleClaims-DotNet." Click on it.
1. On the following page, click on the "Users" tab.  Select any user, click the "Assign" button in the bottom tray, and assign the user to an Application Role.  Repeat this process for any users you would like to have access to Tasks in the application.
1. Sign out of the sample application and sign back in.

## Step 2 : Run the sample
Explore the application by assigning various users and groups to roles via Azure Portal. Login as users in different roles, and notice the differences in functionality available to each.  Each role has different capabilities on the "Tasks" page, as described above.