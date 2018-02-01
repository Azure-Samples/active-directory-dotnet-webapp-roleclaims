# This script creates the Azure AD applications needed for this sample and updates the configuration files
# for the visual Studio projects from the data in the Azure AD applications.
#
# Before running this script you need to install the AzureAD cmdlets as an administrator. 
# For this:
# 1) Run Powershell as an administrator
# 2) in the PowerShell window, type: Install-Module AzureAD
#
# Before you run this script
# 3) With the Azure portal (https://portal.azure.com), choose your active directory tenant, then go to the Properties of the tenant and copy
#    the DirectoryID. This is what we'll use in this script for the tenant ID
# 
# To configurate the applications
# 4) Run the following command:
#      $apps = ConfigureApplications -tenantId [place here the GUID representing the tenant ID]
#    You will be prompted by credentials, be sure to enter credentials for a user who can create applications
#    in the tenant
#
# To execute the samples
# 5) Build and execute the applications. This just works
#
# To cleanup
# 6) Optionnaly if you want to cleanup the applications in the Azure AD, run:
#      CleanUp $apps
#    The applications are un-registered
param([PSCredential]$Credential="", [string]$TenantId="")
Import-Module AzureAD
$ErrorActionPreference = 'Stop'

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




# Updates the config file for a client application
Function UpdateSampleConfigFile([string] $configFilePath, [string] $tenantId, [string] $clientId, [string] $appKey, [string] $baseAddress)
{
    ReplaceSetting -configFilePath $configFilePath -key "ida:Tenant" -newValue $tenantId
    ReplaceSetting -configFilePath $configFilePath -key "ida:ClientId" -newValue $clientId
    ReplaceSetting -configFilePath $configFilePath -key "ida:AppKey" -newValue $appKey
    ReplaceSetting -configFilePath $configFilePath -key "ida:PostLogoutRedirectUri" -newValue $baseAddress
}

# Updates the config file for a client application
Function UpdateServiceConfigFile([string] $configFilePath, [string] $tenantId, [string] $audience)
{
    ReplaceSetting -configFilePath $configFilePath -key "ida:Tenant" -newValue $tenantId
    ReplaceSetting -configFilePath $configFilePath -key "ida:Audience" -newValue $audience
}

# Create an application role of given name and description
Function CreateAppRole([string] $Name, [string] $Description)
{
    $appRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
    $appRole.AllowedMemberTypes = New-Object System.Collections.Generic.List[string]
    $appRole.AllowedMemberTypes.Add("User");
    $appRole.DisplayName = $Name
    $appRole.Id = New-Guid
    $appRole.IsEnabled = $true
    $appRole.Description = $Description
    $appRole.Value = $Name;
    return $appRole
}

# Adds the requiredAccesses (expressed as a pipe separated string) to the requiredAccess structure
# The exposed permissions are in the $exposedPermissions collection, and the type of permission (Scope | Role) is 
# described in $permissionType
Function AddResourcePermission($requiredAccess, `
                               $exposedPermissions, [string]$requiredAccesses, [string]$permissionType)
{
        foreach($permission in $requiredAccesses.Trim().Split("|"))
        {
            foreach($exposedPermission in $exposedPermissions)
            {
                if ($exposedPermission.Value -eq $permission)
                 {
                    $resourceAccess = New-Object Microsoft.Open.AzureAD.Model.ResourceAccess
                    $resourceAccess.Type = $permissionType # Scope = Delegated permissions | Role = Application permissions
                    $resourceAccess.Id = $exposedPermission.Id # Read directory data
                    $requiredAccess.ResourceAccess.Add($resourceAccess)
                 }
            }
        }
}

#
# Exemple: GetRequiredPermissions "Microsoft Graph"  "Graph.Read|User.Read"
# See also: http://stackoverflow.com/questions/42164581/how-to-configure-a-new-azure-ad-application-through-powershell
#
Function GetRequiredPermissions([string] $applicationDisplayName, [string] $requiredDelegatedPermissions, [string]$requiredApplicationPermissions)
{
    $sp = Get-AzureADServicePrincipal -Filter "DisplayName eq '$applicationDisplayName'"
    $appid = $sp.AppId
    $requiredAccess = New-Object Microsoft.Open.AzureAD.Model.RequiredResourceAccess
    $requiredAccess.ResourceAppId = $appid 
    $requiredAccess.ResourceAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.ResourceAccess]

    # $sp.Oauth2Permissions | Select Id,AdminConsentDisplayName,Value: To see the list of all the Delegated permissions for the application:
    if ($requiredDelegatedPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.Oauth2Permissions -requiredAccesses $requiredDelegatedPermissions -permissionType "Scope"
    }
    
    # $sp.AppRoles | Select Id,AdminConsentDisplayName,Value: To see the list of all the Application permissions for the application
    if ($requiredApplicationPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.AppRoles -requiredAccesses $requiredApplicationPermissions -permissionType "Role"
    }
    return $requiredAccess
}

# Create a password that can be used as an application key
Function ComputePassword
{
    $aesManaged = New-Object "System.Security.Cryptography.AesManaged"
    $aesManaged.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aesManaged.Padding = [System.Security.Cryptography.PaddingMode]::Zeros
    $aesManaged.BlockSize = 128
    $aesManaged.KeySize = 256
    $aesManaged.GenerateKey()
    return [System.Convert]::ToBase64String($aesManaged.Key)
}

# Create an application key
# See https://www.sabin.io/blog/adding-an-azure-active-directory-application-and-key-using-powershell/
Function CreateAppKey([DateTime] $fromDate, [int] $durationInYears, [string]$pw)
{
    $endDate = $fromDate.AddYears($durationInYears) 
    $keyId = (New-Guid).ToString();
    $key = New-Object Microsoft.Open.AzureAD.Model.PasswordCredential($null, $fromDate, $keyId, $endDate, $pw) 
}

Function CreateUserRepresentingAppRole([string]$appName, $role, [string]$tenantName)
{
    $password = "test123456789."
    $displayName=$appName+"-"+$role.Value
    $userEmail = $displayName+"@"+$tenantName
    $nickName=$role.Value
    $passwordProfile = New-Object Microsoft.Open.AzureAD.Model.PasswordProfile($password, $false, $false)
    New-AzureADUser -DisplayName $displayName -PasswordProfile $passwordProfile -AccountEnabled $true -MailNickName $nickName -UserPrincipalName $userEmail
}

Function UnCommentSingleTenantAppConditionalDirective($file)
{
    $lines = [System.IO.File]::ReadAllLines($file)
    if ($lines[0].Contains("#define SingleTenantApp") -And $lines[0].StartsWith("// "))
    {
        $lines[0] = $lines[0].Replace("// ", "")
        [System.IO.File]::WriteAllLines($file, $lines)
    }
}

Function ConfigureApplications
{
<#
.Description
This function creates the Azure AD applications for the sample in the provided Azure AD tenant and updates the
configuration files in the client and service project  of the visual studio solution (App.Config and Web.Config)
so that they are consistent with the Applications parameters
#>
    [CmdletBinding()]
    param(
        [PSCredential] $Credential,
        [Parameter(HelpMessage='Tenant ID (This is a GUID which represents the "Directory ID" of the AzureAD tenant into which you want to create the apps')]
        [string] $tenantId
    )

   process
   {
    # $tenantId is the Active Directory Tenant. This is a GUID which represents the "Directory ID" of the AzureAD tenant 
    # into which you want to create the apps. Look it up in the Azure portal in the "Properties" of the Azure AD. 

    # Login to Azure PowerShell (interactive if credentials are not already provided: 
    # you'll need to sign-in with creds enabling your to create apps in the tenant)
    if (!$Credential)
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
    $tenantName =  $tenant.VerifiedDomains[0].Name

    . .\Config.ps1
    $homePage = $homePage + "/"

    # Add application Roles
    $writerRole = CreateAppRole -Name "Writer" -Description "Writers Have the ability to create tasks."
    $observerRole = CreateAppRole -Name "Observer"  -Description "Observers only have the ability to view tasks and their statuses"
    $approverRole = CreateAppRole -Name "Approver" -Description  "Approvers have the ability to change the status of tasks."
    $adminRole = CreateAppRole -Name "Admin" -Description  "Admins can manage roles and perform all task actions."

    $appRoles = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.AppRole]
    $appRoles.Add($writerRole)
    $appRoles.Add($observerRole)
    $appRoles.Add($approverRole)
    $appRoles.Add($adminRole)

    # Add Required Resources Access (MicrosoftGraph)
    $requiredResourcesAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
    $microsoftGraphRequiredPermissions = GetRequiredPermissions -applicationDisplayName "Microsoft Graph" `
                                                                -requiredDelegatedPermissions "Directory.Read.All|User.Read";
    $requiredResourcesAccess.Add($microsoftGraphRequiredPermissions)

    # Get an application key
    $pw = ComputePassword
    $fromDate = [DateTime]::Now
    $key = CreateAppKey -fromDate $fromDate -durationInYears 1 $pw
    $appKey = $pw

    # Create the Azure Active Directory Application and it's service principal
    Write-Host "Creating the AAD appplication ($appName) with 4 roles and accessing Microsoft.Graph"
    $aadApplication = New-AzureADApplication -DisplayName $appName `
                                             -HomePage $homePage `
                                             -ReplyUrls $homePage `
                                             -IdentifierUris $appIdURI `
                                             -LogoutUrl $logoutURI `
                                             -AppRoles $appRoles `
                                             -PasswordCredentials $key `
                                             -RequiredResourceAccess $requiredResourcesAccess
                                         
    $servicePrincipal = New-AzureADServicePrincipal -AppId $aadApplication.AppId
    
    # Update the config file in the application
    $configFile = $pwd.Path + "\WebApp-RoleClaims-DotNet\Web.Config"
    Write-Host "Updating the sample code ($configFile)"
    UpdateSampleConfigFile  -configFilePath $configFile `
                            -clientId $aadApplication.AppId `
                            -appKey $appKey `
                            -tenantId $tenantId `
                            -baseAddress $homePage

    # Update the Startup.Auth.cs file to enable a single-tenant application
    $file = "$pwd\WebApp-RoleClaims-DotNet\App_Start\Startup.Auth.cs"
    Write-Host "Updating the code to run as a single tenant application: '"$file"''"
    UnCommentSingleTenantAppConditionalDirective $file

    # Create
    # ------
    # Make sure that the user who created the application is an admin of the application
    $userPrincipal = $creds.Account.Id
    Write-Host "Enable '$userPrincipal' as an 'admin' of the application"
    $user = Get-AzureADUser -Filter "UserPrincipalName eq '$userPrincipal'"
    $userAssignment = New-AzureADUserAppRoleAssignment -ObjectId $user.ObjectId -PrincipalId $user.ObjectId -ResourceId $servicePrincipal.ObjectId -Id $adminRole.Id

    # Creating an Approver
    Write-Host "Adding an approver user"
    $anApprover = CreateUserRepresentingAppRole $appName $approverRole $tenantName
    $userAssignment = New-AzureADUserAppRoleAssignment -ObjectId $anApprover.ObjectId -PrincipalId $anApprover.ObjectId -ResourceId $servicePrincipal.ObjectId -Id $approverRole.Id
    Write-Host "Created "($anApprover.UserPrincipalName)" with password 'test123456789.'"

    # Creating an Observer
    Write-Host "Adding an observer user"
    $anObserver = CreateUserRepresentingAppRole $appName $observerRole $tenantName
    $userAssignment = New-AzureADUserAppRoleAssignment -ObjectId $anObserver.ObjectId -PrincipalId $anObserver.ObjectId -ResourceId $servicePrincipal.ObjectId -Id $observerRole.Id
    Write-Host "Created "($anObserver.UserPrincipalName)" with password 'test123456789.'"

    # Creating a Writer
    Write-Host "Adding a writer user"
    $aWriter = CreateUserRepresentingAppRole $appName $writerRole $tenantName
    $userAssignment = New-AzureADUserAppRoleAssignment -ObjectId $aWriter.ObjectId -PrincipalId $aWriter.ObjectId -ResourceId $servicePrincipal.ObjectId -Id $writerRole.Id
    Write-Host "Created "($aWriter.UserPrincipalName)" with password 'test123456789.'"

    # Completes
    Write-Host "Done."
   }
}

# Run interactively (will ask you for the tenant ID)
ConfigureApplications -Credential $Credential -tenantId $TenantId


# you can also provide the tenant ID and the credentials
# $tenantId = "ID of your AAD directory"
# $apps = ConfigureApplications -tenantId $tenantId 


# When you have built your Visual Studio solution and ran the code, if you want to clean up the Azure AD applications, just 
# run the following command in the same PowerShell window as you ran ConfigureApplications
# . .\CleanUp -Credentials $Credentials