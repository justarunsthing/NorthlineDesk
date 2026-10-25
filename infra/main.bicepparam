using 'main.bicep'

param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param deployerObjectId = readEnvironmentVariable('AZURE_OBJECT_ID')
param sqlAdminLogin = 'northlineadmin'
param budgetAmount = 40
param budgetEmail = ''
param budgetStart = '2026-10-01'