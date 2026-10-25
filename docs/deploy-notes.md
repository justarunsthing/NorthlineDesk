# First deploy

The Actions tab shows five red Deploy runs before the cutover stayed green. Each one below is a run that failed, the cause, and the change that made the next run pass. The demo red-gate workflow is not in this list. That job finishes red on purpose.

## The federated subject was rejected

Deploy could not log in to Azure. GitHub sent a subject that included the owner id and the repository id, because this repository was created after 15 July 2026. The credential had been registered with the older `repo:owner/name` form.

The two credentials are now `repo:justarunsthing@207272569/NorthlineDesk@1432149576:environment:staging` and the matching production subject. `docs/azure-setup.md` records both.

## The workflow environment never reached Bicep

The next deploy logged in and then failed while compiling `infra/main.bicepparam`. The parameters were not being passed in from the workflow environment.

`scripts/azure.sh` now passes `sqlAdminPassword` and `deployerObjectId` on the deployment command, and the parameter file reads them with `readEnvironmentVariable`.

## A new subscription has no providers and no P0v3 quota

The template then failed because `Microsoft.Web`, `Microsoft.Sql`, `Microsoft.Insights`, `Microsoft.OperationalInsights`, and `Microsoft.KeyVault` were not registered. Those were registered on the subscription. No file changed.

The following deploy failed with `SubscriptionIsOverQuotaForSku`. A free trial has a P0v3 quota of 0, and that quota cannot be raised while the offer is a free trial. The subscription was moved to pay-as-you-go, the spending limit was turned off, and a quota of 1 P0v3 in UK South was requested. The plan size in the template stayed P0v3.

## The availability test rejected the JSON body

The match test posted INV-1001 and the deployment failed with `Value cannot be null. Parameter name: format`. A standard web test stores `RequestBody` as base64.

`infra/main.bicep` now passes `base64('{"invoiceNumber":"INV-1001"}')`.

## The migrator logged in as user 'null'

Schema deployment failed with `Login failed for user 'null'`. The Bicep output for the administrator login was empty, and the script used that empty value as the user name.

The login is now read from the server. When that value is missing, the script uses `northlineadmin`.

## Azure SQL already had the database

The next run failed with error 1801, `Database 'NorthlineDesk' already exists`. `DB_ID` on `master` did not see the existing Azure SQL database, so the migrator issued `CREATE DATABASE` again.

`MigrationRunner` now checks `sys.databases` and creates `NorthlineDesk` only when that name is absent.

## The retired procedure looked like a failed promote

Promote swapped the slot and dropped `dbo.usp_MatchInvoice`. The staging check then failed. `curl -f` stops on HTTP 500, and the staging page returns 500 because the procedure is gone. The swap and the drop had already succeeded.

`scripts/azure.sh` now reads the staging body and continues when that body says the stored procedure is gone. Reset and the following promote both finished green.

## What that sequence showed

The production environment still required a person to approve the swap. The failures were in the template and the script.

The staging 500 is the proof that migration 002 ran. A check that demands HTTP 200 would hide that proof.

The drop has to follow the swap. On the red promote, the database was already cut over before the check failed. The money was not the failure. The assertion was.