// One resource group, UK South, one Windows plan.
// The desk and the match API share the plan. The staging slot is the cutover.
// MatchMode is the only app setting that swaps. Everything else sticks to the slot.

@description('Resource group location. The workflow refuses anything other than uksouth.')
param location string = resourceGroup().location

@description('SQL administrator login. The password is a pipeline parameter, not a file.')
param sqlAdminLogin string = 'northlineadmin'

@secure()
param sqlAdminPassword string

@description('Object id of the GitHub Actions enterprise application, so it can write Key Vault secrets.')
param deployerObjectId string

@description('Monthly budget in GBP. The plan keeps charging until the resource group is deleted.')
param budgetAmount int = 40

@description('Leave empty to skip the budget alert.')
param budgetEmail string = ''

@description('First day of the current month, yyyy-MM-dd.')
param budgetStart string = '2026-10-01'

var token = take(uniqueString(resourceGroup().id), 8)
var planName = 'asp-nldesk-${token}'
var deskName = 'nldesk-${token}'
var apiName = 'nlapi-${token}'
var sqlName = 'sql-nldesk-${token}'
var vaultName = 'kvnldesk${token}'
var workspaceName = 'log-nldesk-${token}'
var insightsName = 'appi-nldesk-${token}'
var tenantId = subscription().tenantId
var sqlConnection = '@Microsoft.KeyVault(VaultName=${vaultName};SecretName=sql-connection)'
var shadowKey = '@Microsoft.KeyVault(VaultName=${vaultName};SecretName=shadow-key)'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: { project: 'northlinedesk' }
  sku: {
    name: 'P0v3'
    tier: 'PremiumV3'
    capacity: 1
  }
  kind: 'windows'
  properties: {
    reserved: false
  }
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: json('0.1')
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource azureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: 'NorthlineDesk'
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
    requestedBackupStorageRedundancy: 'Local'
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

resource desk 'Microsoft.Web/sites@2023-12-01' = {
  name: deskName
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      netFrameworkVersion: 'v4.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      use32BitWorkerProcess: false
      http20Enabled: true
      defaultDocuments: [ 'Default.aspx' ]
      metadata: [
        { name: 'CURRENT_STACK', value: 'dotnetfx' }
      ]
    }
  }
}

resource staging 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: desk
  name: 'staging'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    enabled: true
    httpsOnly: true
    serverFarmId: plan.id
    siteConfig: {
      netFrameworkVersion: 'v4.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      use32BitWorkerProcess: false
      http20Enabled: true
      metadata: [
        { name: 'CURRENT_STACK', value: 'dotnetfx' }
      ]
    }
  }
}

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: apiName
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      use32BitWorkerProcess: false
      http20Enabled: true
      // The platform interval is one minute and is not configurable. /health must not open SQL.
      healthCheckPath: '/health'
      metadata: [
        { name: 'CURRENT_STACK', value: 'dotnet' }
      ]
    }
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    tenantId: tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: false
    enableSoftDelete: true
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: deployerObjectId
        permissions: { secrets: [ 'get', 'list', 'set' ] }
      }
      {
        tenantId: tenantId
        objectId: desk.identity.principalId
        permissions: { secrets: [ 'get' ] }
      }
      {
        tenantId: tenantId
        objectId: staging.identity.principalId
        permissions: { secrets: [ 'get' ] }
      }
      {
        tenantId: tenantId
        objectId: api.identity.principalId
        permissions: { secrets: [ 'get' ] }
      }
    ]
  }
}

var deskSettings = {
  MatchMode: 'Procedure'
  MatchApiUrl: 'https://${api.properties.defaultHostName}/match'
  SqlConnection: sqlConnection
  APPLICATIONINSIGHTS_CONNECTION_STRING: insights.properties.ConnectionString
  ApplicationInsightsAgent_EXTENSION_VERSION: '~2'
  XDT_MicrosoftApplicationInsights_Mode: 'default'
  WEBSITE_RUN_FROM_PACKAGE: '1'
}

resource deskAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: desk
  name: 'appsettings'
  properties: deskSettings
}

resource stagingAppSettings 'Microsoft.Web/sites/slots/config@2023-12-01' = {
  parent: staging
  name: 'appsettings'
  properties: union(deskSettings, { MatchMode: 'Api' })
}

resource stickySettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: desk
  name: 'slotConfigNames'
  properties: {
    appSettingNames: [
      'MatchApiUrl'
      'SqlConnection'
      'APPLICATIONINSIGHTS_CONNECTION_STRING'
      'ApplicationInsightsAgent_EXTENSION_VERSION'
      'XDT_MicrosoftApplicationInsights_Mode'
      'WEBSITE_RUN_FROM_PACKAGE'
    ]
  }
}

resource apiAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: api
  name: 'appsettings'
  properties: {
    // ASP.NET Core maps this name to ConnectionStrings:Northline.
    // The desk is Framework configuration, so it uses SqlConnection instead.
    ConnectionStrings__Northline: sqlConnection
    ShadowKey: shadowKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: insights.properties.ConnectionString
    ApplicationInsightsAgent_EXTENSION_VERSION: '~2'
    XDT_MicrosoftApplicationInsights_Mode: 'default'
    WEBSITE_RUN_FROM_PACKAGE: '1'
  }
}

var matchUrl = 'https://${desk.properties.defaultHostName}/Match.aspx?invoice=INV-1001'
var matchPostUrl = 'https://${api.properties.defaultHostName}/match'

// Microsoft's population tag for the UK South availability location.
// The tag is historical. The display name in the portal is UK South.
var ukSouthTestLocation = 'emea-ru-msa-edge'

resource matchPageTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: 'nldesk-match-${token}'
  location: location
  kind: 'standard'
  tags: {
    'hidden-link:${insights.id}': 'Resource'
  }
  properties: {
    SyntheticMonitorId: 'nldesk-match-${token}'
    Name: 'Northline match page'
    Description: 'GET the production Match page. Disabled: a standard test cannot run less often than every 5 minutes, so it cannot leave a 60 minute gap for auto-pause. The daily check is .github/workflows/daily-live.yml.'
    Enabled: false
    Frequency: 900
    Timeout: 30
    Kind: 'standard'
    RetryEnabled: true
    Locations: [
      { Id: ukSouthTestLocation }
    ]
    Request: {
      RequestUrl: matchUrl
      HttpVerb: 'GET'
      ParseDependentRequests: false
      FollowRedirects: true
    }
    ValidationRules: {
      ExpectedHttpStatusCode: 200
      SSLCheck: true
    }
  }
}

resource matchPostTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: 'nlapi-match-${token}'
  location: location
  kind: 'standard'
  tags: {
    'hidden-link:${insights.id}': 'Resource'
  }
  properties: {
    SyntheticMonitorId: 'nlapi-match-${token}'
    Name: 'Northline POST match'
    Description: 'POST INV-1001 to the match service. The service approves £612.00. Disabled for the same reason as the Match page test.'
    Enabled: false
    Frequency: 900
    Timeout: 30
    Kind: 'standard'
    RetryEnabled: true
    Locations: [
      { Id: ukSouthTestLocation }
    ]
    Request: {
      RequestUrl: matchPostUrl
      HttpVerb: 'POST'
      RequestBody: '{"invoiceNumber":"INV-1001"}'
      ParseDependentRequests: false
      FollowRedirects: true
      Headers: [
        { key: 'Content-Type', value: 'application/json' }
      ]
    }
    ValidationRules: {
      ExpectedHttpStatusCode: 200
      SSLCheck: true
      ContentValidation: {
        ContentMatch: '612.00'
        IgnoreCase: false
        PassIfTextFound: true
      }
    }
  }
}

resource budget 'Microsoft.Consumption/budgets@2024-08-01' = if (!empty(budgetEmail)) {
  name: 'northlinedesk-monthly'
  properties: {
    timePeriod: {
      startDate: budgetStart
      endDate: '2027-10-01'
    }
    timeGrain: 'Monthly'
    amount: budgetAmount
    category: 'Cost'
    notifications: {
      near: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 80
        contactEmails: [ budgetEmail ]
        thresholdType: 'Actual'
      }
    }
  }
}

output deskName string = desk.name
output apiName string = api.name
output sqlServerName string = sqlServer.name
output sqlAdminLogin string = sqlAdminLogin
output vaultName string = vault.name
output deskHost string = desk.properties.defaultHostName
output apiHost string = api.properties.defaultHostName
output stagingHost string = '${desk.name}-staging.azurewebsites.net'
output matchUrl string = matchUrl
output databaseName string = sqlDatabase.name
output firewallRule string = azureServices.name
output matchTest string = matchPageTest.name
output stickySettingCount int = length(stickySettings.properties.appSettingNames)
output budgetName string = empty(budgetEmail) ? '' : budget.name
