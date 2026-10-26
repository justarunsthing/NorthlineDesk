# Cost to serve

Exported from Cost analysis for resource group `rg-northlinedesk-uks` on the morning of 26 October 2026. The file has usage for 25 October 2026 only. There are no rows yet for 26 October.

| Field | Value |
| --- | --- |
| Date range | 25 October 2026 |
| Currency | GBP |
| Total | 5.43 |
| App Service plan | 1.56 (`asp-nldesk-nar6kqrn`, meter P0v3 App) |
| Azure SQL | 3.87 (`sql-nldesk-nar6kqrn` / `northlinedesk`: vCore 3.80, data stored 0.07) |
| Other meters | Key Vault operations 0.0002. Bandwidth, Log Analytics, the desk, the staging slot, and the match API are 0.00. |

The desk, the staging slot, and the match API are on the App Service plan, so their own rows are 0.00. The plan is the charge that continues until the resource group is deleted. SQL General Purpose serverless can pause after 60 idle minutes, which is the shortest delay Azure allows. While the group is up, `/health` and the availability tests query SQL, so that pause does not get a quiet hour. One test GETs the Match page. The other POSTs INV-1001 to `/match`.

P0v3 is the smallest Premium v3 size. Slots start at Standard. Premium v3 is the current generation, and this plan has to host Framework 4.8 and the .NET 10 service together. The slot has no separate charge. It shares the one core.