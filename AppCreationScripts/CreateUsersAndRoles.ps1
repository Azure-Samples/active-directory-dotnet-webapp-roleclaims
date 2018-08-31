[CmdletBinding()]
param(
    [PSCredential] $Credential,
    [Parameter(Mandatory=$False, HelpMessage='Tenant ID (This is a GUID which represents the "Directory ID" of the AzureAD tenant into which you want to create the apps')]
    [string] $tenantId
)

<#
 This script creates the following artefacts in the Azure AD tenant.
 1) A number of App roles
 2) A set of users and assigns them to the app roles.

 Before running this script you need to install the AzureAD cmdlets as an administrator. 
 For this:
 1) Run Powershell as an administrator
 2) in the PowerShell window, type: Install-Module AzureAD

 There are four ways to run this script. For more information, read the AppCreationScripts.md file in the same folder as this script.
#>
Import-Module AzureAD
$ErrorActionPreference = 'Stop'

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

Function CreateUserRepresentingAppRole([string]$appName, $role, [string]$tenantName)
{
    $password = "test123456789."
    $displayName=$appName+"-"+$role.Value
    $userEmail = $displayName+"@"+$tenantName
    $nickName=$role.Value
    CreateUser -displayName $displayName -nickName $nickName -tenantName $tenantName
}

Function CreateUser([string]$displayName, [string]$nickName, [string]$tenantName)
{
    $password = "test123456789."
    $userEmail = $displayName+"@"+$tenantName
    $passwordProfile = New-Object Microsoft.Open.AzureAD.Model.PasswordProfile($password, $false, $false)
    New-AzureADUser -DisplayName $displayName -PasswordProfile $passwordProfile -AccountEnabled $true -MailNickName $nickName -UserPrincipalName $userEmail
}

Function CreateRolesUsersAndRoleAssignments
{
<#.Description
   This function creates the 
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

    # Get the user running the script
    $user = Get-AzureADUser -ObjectId $creds.Account.Id

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
    
    # Add the roles
    Write-Host "Adding app roles to to the app 'TaskTrackerWebApp-RoleClaims' in tenant '$tenantName'"

    $app = Get-AzureADApplication -Filter "identifierUris/any(uri:uri eq 'https://$tenantName/TaskTrackerWebApp-RoleClaims')"  
    
    if ($app)
    {
        $servicePrincipal = Get-AzureADServicePrincipal -Filter "AppId eq '$($app.AppId)'"  
        
        Set-AzureADApplication -ObjectId $app.ObjectId -AppRoles $appRoles
        Write-Host "Successfully added app roles to the app 'TaskTrackerWebApp-RoleClaims'."

        $appName = $app.DisplayName

        Write-Host "Creating users and assigning them to roles."

        # Create users
        # ------
        # Make sure that the user who created the application is an admin of the application
        Write-Host "Enable '$($user.DisplayName)' as an 'admin' of the application"
        $userAssignment = New-AzureADUserAppRoleAssignment -ObjectId $user.ObjectId -PrincipalId $user.ObjectId -ResourceId $servicePrincipal.ObjectId -Id $adminRole.Id

        # Creating an Approver
        Write-Host "Adding an user and assigning to '$($approverRole.Name)' role"
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

    }
    else {
        Write-Host "Failed to add app roles to the app 'TaskTrackerWebApp-RoleClaims'."
    }

    Write-Host "Run the ..\\CleanupUsers.ps1 command to remove users created for this sample's application ."
}

CreateRolesUsersAndRoleAssignments -Credential $Credential -tenantId $TenantId
