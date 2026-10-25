# Slot flow

One Windows plan in UK South hosts the desk, the staging slot, and the match API. The slot has no plan of its own. Azure SQL and Key Vault sit beside the plan. Both slots read SQL through the same Key Vault reference.

`MatchMode` is the setting a swap carries. Everything in `slotConfigNames` stays where it was.

## What shares the plan

```mermaid
flowchart TB
  plan["App Service plan P0v3"]
  desk["Desk production\nMatchMode Procedure"]
  staging["Desk staging\nMatchMode Api"]
  api["Match API\nno slot"]
  vault["Key Vault"]
  sql["Azure SQL NorthlineDesk"]

  plan --> desk
  plan --> staging
  plan --> api
  vault --> desk
  vault --> staging
  vault --> api
  sql --> desk
  sql --> api
```

Production calls `dbo.usp_MatchInvoice` and shows £1,224.00. Staging calls `POST /match` and shows £612.00. The API has no slot. Its connection setting is `ConnectionStrings__Northline`, the ASP.NET Core name for the same Key Vault reference.

## What a swap exchanges

```mermaid
sequenceDiagram
  participant Prod as Production
  participant Swap as Slot swap
  participant Stage as Staging

  Note over Prod: MatchMode Procedure
  Note over Stage: MatchMode Api
  Prod->>Swap: swap staging into production
  Swap->>Prod: MatchMode Api
  Swap->>Stage: MatchMode Procedure
```

`SqlConnection`, `MatchApiUrl`, Application Insights, and `WEBSITE_RUN_FROM_PACKAGE` are in `slotConfigNames`. Those six settings do not move. After the swap, production calls the match service. Migration 002 then drops the procedure, so the staging slot, now on `Procedure`, reports that the procedure is gone.

The command that performs this swap is the promote job in `.github/workflows/deploy.yml`.