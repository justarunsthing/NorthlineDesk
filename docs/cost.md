# Cost to serve

Read from Cost analysis for resource group `rg-northlinedesk-uks` on the evening of 26 October 2026, about 8:56pm UK time. The Azure account was opened on Sunday 25 October 2026, so this is the second day. The 23 October mark on the chart is an axis label. Cost analysis lags, so the Monday portion can still move.

| Field | Value |
| --- | --- |
| Signed up | Sunday 25 October 2026 |
| Reading | Monday 26 October 2026, evening |
| How long it had been running | 2 days |
| Currency | GBP |
| Total | 10.97 |
| Azure SQL | 7.93 (`sql-nldesk-nar6kqrn` / `northlinedesk`: General Purpose compute 7.80, data stored 0.13) |
| App Service plan | 2.95 (`asp-nldesk-nar6kqrn`, meter P0v3 App) |
| Other meters | Azure Monitor 0.09. Key Vault under 0.01. Bandwidth, the desk, the staging slot, and the match API are 0.00. |

Sunday 25 October, the day the account was opened, exported as 5.43: the plan 1.56, SQL 3.87 (vCore 3.80 and data stored 0.07), and Key Vault operations 0.0002. Monday evening's total is 10.97, so Monday so far is the other 5.54.

The desk, the staging slot, and the match API are on the App Service plan, so their own rows are 0.00. The plan is the charge that continues until the resource group is deleted. Across both days the database stayed awake: `/health` ran the match query every minute, and two availability tests called the Match page and `POST /match` every 15 minutes, so the 60 minute auto-pause never started. The vCore portion of that is the 7.80. Storage is the 0.13.

The repo change stops those probes from opening SQL. It is not included in this reading. After that change is deployed, an hour with no visitor stops the vCore meter. Storage remains. The first request after a pause waits up to 90 seconds for the database to resume.

P0v3 is the smallest Premium v3 size. Slots start at Standard. Premium v3 is the current generation, and this plan has to host Framework 4.8 and the .NET 10 service together. The slot has no separate charge. It shares the one core.