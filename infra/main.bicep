// =====================================================================================
// QualyHub — Infraestrutura Azure (escopo: resource group)
// Custo sob controle: App Service B1 (Web), Azure SQL serverless
// com auto-pause, Storage LRS e orçamento com alerta. Escalar só quando o uso justificar.
// =====================================================================================
targetScope = 'resourceGroup'

@description('Prefixo dos recursos (minúsculo, sem espaços).')
@maxLength(10)
param prefixo string = 'qualyhub'

@allowed([ 'dev', 'hml', 'prd' ])
param ambiente string = 'prd'

@description('Região. Brazil South mantém os dados no Brasil (LGPD) e reduz latência.')
param local string = 'brazilsouth'

@description('ObjectId do GRUPO do Entra ID administrador do Azure SQL (ex.: "QualyHub-DBA").')
param sqlAdminGrupoObjectId string

@description('Nome de exibição do grupo administrador do SQL.')
param sqlAdminGrupoNome string

@description('SKU do plano. B1 atende o MVP; subir para B2/S1/P0v3 quando houver uso real.')
@allowed([ 'F1', 'B1', 'B2', 'B3', 'S1', 'P0v3', 'P1v3' ])
param appServiceSku string = 'B1'

@description('vCores máximos do SQL serverless (0.5 a 2 cobre o MVP).')
param sqlMaxVCores int = 1

@description('Minutos sem uso até pausar o banco (mín. 60). -1 desativa o auto-pause.')
param sqlAutoPauseMinutos int = 60

@description('Orçamento mensal (moeda da assinatura) para alerta de custo.')
param orcamentoMensal int = 500

@description('E-mails que recebem alertas de orçamento.')
param emailsAlerta array

@description('Cria o orçamento com alerta de custo (desligue se a assinatura não suportar Consumption budgets).')
param criarOrcamento bool = true

@description('Início do orçamento: primeiro dia do mês corrente (yyyy-MM-01).')
param orcamentoInicio string

@description('ClientId (Application ID) do registro do app no Entra ID usado no login. Vazio = site sobe mas recusa iniciar em produção.')
param entraClientId string = ''

@description('Tenant do Entra ID do login (padrão: o tenant da assinatura).')
param entraTenantId string = subscription().tenantId

var sufixo = uniqueString(resourceGroup().id)
var nome = '${prefixo}-${ambiente}'
var tags = { sistema: 'QualyHub', ambiente: ambiente, gerenciadoPor: 'bicep' }

// ---------------------------- Observabilidade ---------------------------------------
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${nome}'
  location: local
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: 1 } // teto diário: evita surpresa de custo com log
  }
}

resource appi 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${nome}'
  location: local
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    SamplingPercentage: 50
  }
}

// ---------------------------- Key Vault ----------------------------------------------
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${prefixo}-${ambiente}-${take(sufixo, 6)}'
  location: local
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
  }
}

// ---------------------------- Storage (documentos e evidências) ---------------------
resource st 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('st${prefixo}${ambiente}${sufixo}', 24)
  location: local
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // a aplicação usa só Managed Identity
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobSvc 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: st
  name: 'default'
  properties: {
    isVersioningEnabled: true                                   // cada versão de arquivo preservada
    deleteRetentionPolicy: { enabled: true, days: 30 }          // lixeira de 30 dias
    containerDeleteRetentionPolicy: { enabled: true, days: 30 }
    changeFeed: { enabled: true, retentionInDays: 90 }          // trilha de alterações dos arquivos
  }
}

// documentos: certidões/fornecedores | sgq: MQ/PO/PEIS | evidencias: fotos FVS/FVM
// sensivel: ASO e afins (LGPD, acesso restrito) | importacoes: arquivos do ERP
var containers = [ 'documentos', 'sgq', 'evidencias', 'sensivel', 'importacoes' ]
resource cont 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [for c in containers: {
  parent: blobSvc
  name: c
  properties: { publicAccess: 'None' }
}]

// Custo: fotos e imports antigos vão para camada Cool/Cold automaticamente.
resource ciclo 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: st
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'evidencias-para-cool'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: { blobTypes: [ 'blockBlob' ], prefixMatch: [ 'evidencias/', 'importacoes/' ] }
            actions: {
              baseBlob: {
                tierToCool: { daysAfterModificationGreaterThan: 90 }
                tierToCold: { daysAfterModificationGreaterThan: 365 }
              }
              version: { delete: { daysAfterCreationGreaterThan: 365 } }
            }
          }
        }
      ]
    }
  }
}

// ---------------------------- Azure SQL (serverless) ---------------------------------
resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-${nome}-${take(sufixo, 6)}'
  location: local
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true // sem usuário/senha SQL: só Entra ID e Managed Identity
      login: sqlAdminGrupoNome
      sid: sqlAdminGrupoObjectId
      principalType: 'Group'
      tenantId: subscription().tenantId
    }
  }
}

resource sqlFwAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sql
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource db 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sql
  name: 'sqldb-${prefixo}'
  location: local
  tags: tags
  sku: { name: 'GP_S_Gen5', tier: 'GeneralPurpose', family: 'Gen5', capacity: sqlMaxVCores }
  properties: {
    autoPauseDelay: sqlAutoPauseMinutos
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368 // 32 GB
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
    collation: 'Latin1_General_100_CI_AI_SC_UTF8'
  }
}

resource dbBackup 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2023-08-01-preview' = {
  parent: db
  name: 'default'
  properties: { retentionDays: 14 }
}

// ---------------------------- App Service (Web) --------------------------------------
resource plano 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-${nome}'
  location: local
  tags: tags
  kind: 'linux'
  sku: { name: appServiceSku }
  properties: { reserved: true }
}

var sqlConn = 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${db.name};Authentication=Active Directory Default;Encrypt=True;Connect Timeout=60'

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-${nome}-${take(sufixo, 6)}'
  location: local
  tags: tags
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plano.id
    httpsOnly: true
    clientAffinityEnabled: true // Blazor Server: mantém o circuito SignalR na mesma instância
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: appServiceSku != 'F1' // F1 (gratuito) não permite Always On
      webSocketsEnabled: true      // Blazor Server
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/saude'
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appi.properties.ConnectionString }
        { name: 'QualyHub__KeyVaultUri', value: kv.properties.vaultUri }
        { name: 'QualyHub__BlobUri', value: st.properties.primaryEndpoints.blob }
        { name: 'ASPNETCORE_ENVIRONMENT', value: ambiente == 'prd' ? 'Production' : 'Staging' }
        { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' } // TLS termina no proxy do App Service
        { name: 'TZ', value: 'America/Sao_Paulo' }
        { name: 'AzureAd__Instance', value: environment().authentication.loginEndpoint }
        { name: 'AzureAd__TenantId', value: entraTenantId }
        { name: 'AzureAd__ClientId', value: entraClientId }
        { name: 'AzureAd__CallbackPath', value: '/signin-oidc' }
      ]
      connectionStrings: [
        { name: 'QualyHub', connectionString: sqlConn, type: 'SQLAzure' }
      ]
    }
  }
}

// ---------------------------- Permissões (RBAC, sem chaves na aplicação) -------------
var roles = {
  blobContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  kvSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
}

resource rWebBlob 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: st
  name: guid(st.id, web.id, roles.blobContributor)
  properties: { principalId: web.identity.principalId, principalType: 'ServicePrincipal', roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.blobContributor) }
}

resource rWebKv 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, web.id, roles.kvSecretsUser)
  properties: { principalId: web.identity.principalId, principalType: 'ServicePrincipal', roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.kvSecretsUser) }
}

// ---------------------------- Orçamento e alerta de custo ---------------------------
resource orcamento 'Microsoft.Consumption/budgets@2023-11-01' = if (criarOrcamento) {
  name: 'orc-${nome}'
  properties: {
    category: 'Cost'
    amount: orcamentoMensal
    timeGrain: 'Monthly'
    timePeriod: { startDate: orcamentoInicio }
    notifications: {
      real80: { enabled: true, operator: 'GreaterThanOrEqualTo', threshold: 80, thresholdType: 'Actual', contactEmails: emailsAlerta }
      real100: { enabled: true, operator: 'GreaterThanOrEqualTo', threshold: 100, thresholdType: 'Actual', contactEmails: emailsAlerta }
      previsto100: { enabled: true, operator: 'GreaterThanOrEqualTo', threshold: 100, thresholdType: 'Forecasted', contactEmails: emailsAlerta }
    }
  }
}

// ---------------------------- Saídas -------------------------------------------------
output webAppNome string = web.name
output webAppUrl string = 'https://${web.properties.defaultHostName}'
output sqlServidor string = sql.properties.fullyQualifiedDomainName
output sqlBanco string = db.name
output keyVaultNome string = kv.name
output storageNome string = st.name
output webIdentidade string = web.name
