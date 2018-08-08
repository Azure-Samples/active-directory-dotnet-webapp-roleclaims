[CmdletBinding()]
param(
    [PSCredential] $Credential,
    [Parameter(HelpMessage='Tenant ID (This is a GUID which represents the "Directory ID" of the AzureAD tenant into which you want to create the apps')]
    [string] $tenantId
)

<#
 This script creates the Azure AD applications needed for this sample and updates the configuration files
 for the visual Studio projects from the data in the Azure AD applications.

 Before running this script you need to install the AzureAD cmdlets as an administrator. 
 For this:
 1) Run Powershell as an administrator
 2) in the PowerShell window, type: Install-Module AzureAD

 There are four ways to run this script. For more information, read the AppCreationScripts.md file in the same folder as this script.
#>

# Replace the value of an appsettings of a given key in an XML App.Config file.
Function ReplaceSetting([string] $configFilePath, [string] $key, [string] $newValue)
{
    [xml] $content = Get-Content $configFilePath
    $appSettings = $content.configuration.appSettings; 
    $keyValuePair = $appSettings.SelectSingleNode("descendant::add[@key='$key']")
    if ($keyValuePair)
    {
        $keyValuePair.value = $newValue;
    }
    else
    {
        Throw "Key '$key' not found in file '$configFilePath'"
    }
   $content.save($configFilePath)
}


Set-Content -Value "<html><body><table>" -Path createdApps.html
Add-Content -Value "<thead><tr><th>Application</th><th>AppId</th><th>Url in the Azure portal</th></tr></thead><tbody>" -Path createdApps.html

Function ConfigureApplications
{
<#.Description
   This function creates the Azure AD applications for the sample in the provided Azure AD tenant and updates the
   configuration files in the client and service project  of the visual studio solution (App.Config and Web.Config)
   so that they are consistent with the Applications parameters
#> 

    # $tenantId is the Active Directory Tenant. This is a GUID which represents the "Directory ID" of the AzureAD tenant
    # into which you want to create the apps. Look it up in the Azure portal in the "Properties" of the Azure AD.

    # Login to Azure PowerShell (interactive if credentials are not already provided:
    # you'll need to sign-in with creds enabling your to create apps in the tenant)
    if (!$Credential -and $TenantId)
    {
        $creds = Connect-AzureAD -TenantId $tenantId
    }
    else
    {
        if (!$TenantId)
        {
            $creds = Connect-AzureAD -Credential $Credential
        }
        else
        {
            $creds = Connect-AzureAD -TenantId $tenantId -Credential $Credential
        }
    }

    if (!$tenantId)
    {
        $tenantId = $creds.Tenant.Id
    }

    $tenant = Get-AzureADTenantDetail
    $tenantName =  ($tenant.VerifiedDomains | Where { $_._Default -eq $True }).Name

    $perm = [Microsoft.Open.AzureAD.Model.RequiredResourceAccess]@{
    ResourceAppId  = "00000002-0000-0000-c000-000000000000"; # Windows Azure Active Directory/Microsoft.Azure.ActiveDirectory
    ResourceAccess = [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
        Id   = "311a71cc-e848-46a1-bdf8-97ff7156d8e6"; #access scope: Delegated permission to sign in and read user profile
        Type = "Scope"
        }
    }
   # Create the service AAD application
   Write-Host "Creating the AAD application (TaskTrackerWebApp-RoleClaims)"
   $serviceAadApplication = New-AzureADApplication -DisplayName "TaskTrackerWebApp-RoleClaims" `
                                                   -HomePage "https://localhost:44322/" `
                                                   -LogoutUrl "https://localhost:44322/Account/EndSession" `
                                                   -ReplyUrls "https://localhost:44322/" `
                                                   -IdentifierUris "https://$tenantName/TaskTrackerWebApp-RoleClaims" `
                                                   -RequiredResourceAccess $perm `
                                                   -PublicClient $False


   $currentAppId = $serviceAadApplication.AppId
   $serviceServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId -Tags {WindowsAzureActiveDirectoryIntegratedApp}

   # add this user as app owner
   $user = Get-AzureADUser -ObjectId $creds.Account.Id
   Add-AzureADApplicationOwner -ObjectId $serviceAadApplication.ObjectId -RefObjectId $user.ObjectId
   Write-Host "'$($user.UserPrincipalName)' added as an application owner to app '$($serviceServicePrincipal.DisplayName)'"

   Write-Host "Done creating the service application (TaskTrackerWebApp-RoleClaims)"

   # URL of the AAD application in the Azure portal
   $servicePortalUrl = "https://portal.azure.com/#@"+$tenantName+"/blade/Microsoft_AAD_IAM/ApplicationBlade/appId/"+$serviceAadApplication.AppId+"/objectId/"+$serviceAadApplication.ObjectId
   Add-Content -Value "<tr><td>service</td><td>$currentAppId</td><td><a href='$servicePortalUrl'>TaskTrackerWebApp-RoleClaims</a></td></tr>" -Path createdApps.html


   # Update config file for 'service'
   $configFile = $pwd.Path + "\..\WebApp-RoleClaims-DotNet\Web.Config"
   Write-Host "Updating the sample code ($configFile)"
   ReplaceSetting -configFilePath $configFile -key "ida:ClientId" -newValue $serviceAadApplication.AppId
   ReplaceSetting -configFilePath $configFile -key "ida:Domain" -newValue $tenantName
   ReplaceSetting -configFilePath $configFile -key "ida:TenantId" -newValue $tenantId
   ReplaceSetting -configFilePath $configFile -key "ida:PostLogoutRedirectUri" -newValue $serviceAadApplication.HomePage
   Write-Host ""
   Write-Host "IMPORTANT: Think of completing the following manual step(s) in the Azure portal":
   Write-Host "- For 'service'"
   Write-Host "  - Navigate to '$servicePortalUrl'"
   Write-Host "  - Refer to the 'Define your application roles' section in README on how to configure your newly created app further."

   Add-Content -Value "</tbody></table></body></html>" -Path createdApps.html  
}


# Run interactively (will ask you for the tenant ID)
ConfigureApplications -Credential $Credential -tenantId $TenantId