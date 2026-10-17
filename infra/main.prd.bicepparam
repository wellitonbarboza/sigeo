using './main.bicep'

// Preencha antes do primeiro deploy (o deploy.sh também consegue preencher os dois primeiros).
param prefixo = 'qualyhub'
param ambiente = 'prd'
param local = 'brazilsouth'
param sqlAdminGrupoObjectId = '<OBJECT-ID-DO-GRUPO-QualyHub-DBA>'
param sqlAdminGrupoNome = 'QualyHub-DBA'
param appServiceSku = 'B1'
param sqlPlano = 'S1' // DTU 20, custo fixo, 250 GB; 'Serverless' volta ao vCore que pausa
param sqlMaxVCores = 1
param sqlAutoPauseMinutos = 60
param orcamentoMensal = 500
param emailsAlerta = [ 'welliton@exemplo.com.br' ]
param orcamentoInicio = '2026-10-01'
