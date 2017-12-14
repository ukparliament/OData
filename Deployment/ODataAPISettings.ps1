<#
.SYNOPSIS
Sets settings for API app.

.DESCRIPTION
Sets values of application settings.

.PARAMETER APIResourceGroupName
Name of the Resource Group where the API Management is.

.NOTES
This script is for use as a part of deployment in VSTS only.
#>

Param(
	[Parameter(Mandatory=$true)] [string] $APIResourceGroupName,
	[Parameter(Mandatory=$true)] [string] $APIManagementName,
	[Parameter(Mandatory=$true)] [string] $ODataAPIName,
	[Parameter(Mandatory=$true)] [string] $APIPrefix
)
$ErrorActionPreference = "Stop"

function Log([Parameter(Mandatory=$true)][string]$LogText){
    Write-Host ("{0} - {1}" -f (Get-Date -Format "HH:mm:ss.fff"), $LogText)
}

Log "Get API Management context"
$management=New-AzureRmApiManagementContext -ResourceGroupName $APIResourceGroupName -ServiceName $APIManagementName
Log "Retrives subscription"
$apiProduct=Get-AzureRmApiManagementProduct -Context $management -Title "$APIPrefix - Parliament [OData]"
$subscription=Get-AzureRmApiManagementSubscription -Context $management -ProductId $apiProduct.ProductId

Log "Gets current settings"
$webApp = Get-AzureRmwebApp -ResourceGroupName $APIResourceGroupName -Name $ODataAPIName
$connectionStrings=$webApp.SiteConfig.ConnectionStrings
$webAppSettings = $webApp.SiteConfig.AppSettings
$settings=@{}
foreach($set in $webAppSettings){ 
    $settings[$set.Name]=$set.Value
}

$connections = @{}
foreach($connection in $connectionStrings){
	if ($connection.Name -ne "SparqlEndpoint") {
		$connections[$connection.Name]=@{Type=if ($connection.Type -eq $null){"Custom"}else{$connection.Type.ToString()};Value=$connection.ConnectionString}
	}
}

Log "Sets new data connection"
$connections["SparqlEndpoint"]=@{Type="Custom";Value="https://$APIManagementName.azure-api.net/$APIPrefix/sparql-endpoint/master?subscription-key=$($subscription.PrimaryKey)"}
Log "Sets new external API url address"
$settings["ExternalAPIAddress"] = "https://api.parliament.uk/$APIPrefix/odata/"
Set-AzureRmWebApp -ResourceGroupName $APIResourceGroupName -Name $ODataAPIName -ConnectionStrings $connections

Log "Job well done!"