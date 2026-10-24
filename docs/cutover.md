# How the cutover works

Both slots run the same desk build. `Match.aspx.cs` reads `MatchMode`.

| Slot, before the swap | MatchMode | What the page calls |
| --- | --- | --- |
| Production | Procedure | `dbo.usp_MatchInvoice` |
| Staging | Api | `POST /match` on the match service |

`MatchMode` is not in `slotConfigNames`. A slot swap exchanges it. After the swap, production is `Api` and staging is `Procedure`.

These settings are sticky, so the swap does not take them with it:

- `SqlConnection` (a Key Vault reference, not the password)
- `MatchApiUrl`
- the Application Insights settings
- `WEBSITE_RUN_FROM_PACKAGE`

The desk reads `SqlConnection` from `ConfigurationManager.AppSettings`. The match service has no slot. It reads `ConnectionStrings__Northline`, which ASP.NET Core maps to `ConnectionStrings:Northline`. Both values are the same Key Vault reference. An unresolved reference still contains `@Microsoft.KeyVault`, and both apps refuse to treat that text as a connection string.

The swap is a human action. The `promote` job uses the GitHub Environment `production`, which needs a required reviewer. The agent boundary forbids the swap.

Order inside that job:

1. Apply `db/explanations/partial-receipt.sql`.
2. Post `/shadow/INV-1001`. Continue only when `promotionBlocked` is false. The allowed difference is £1,224.00 against £612.00, unmatched 0 against 6, ticket `partial-receipt`.
3. Swap staging into production.
4. Apply migration 002, which drops `dbo.usp_MatchInvoice`.
5. Assert `OBJECT_ID(N'dbo.usp_MatchInvoice', N'P')` is null.
6. Check production shows the match service and £612.00. Check staging says the procedure is gone.

Dropping the procedure before the swap would break the production page, which still calls it. The reset action restores the procedure, clears the shadow rows and the ticket, and puts `MatchMode` back. Reset is also behind the production environment, because it changes production.