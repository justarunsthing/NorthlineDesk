# Cost to serve

Read from Cost analysis for resource group `rg-northlinedesk-uks` at 05:40 UK time on Tuesday 27 October 2026. The Azure account was opened on Sunday 25 October 2026, so this is the third day. The 23 October mark on the chart is an axis label. Cost analysis lags, so Tuesday's portion can still move.

| Field | Value |
| --- | --- |
| Signed up | Sunday 25 October 2026 |
| Reading | Tuesday 27 October 2026, 05:40 UK time |
| How long it had been running | 3 days |
| Currency | GBP |
| Total | 14.35 |
| Azure SQL | 10.31 |
| App Service plan | 3.95 (`asp-nldesk-nar6kqrn`, meter P0v3 App) |
| Other meters | Azure Monitor 0.09. Key Vault under 0.01. Bandwidth, the desk, the staging slot, and the match API are 0.00. |

Sunday 25 October, the day the account was opened, exported as 5.43: the plan 1.56, SQL 3.87 (vCore 3.80 and data stored 0.07), and Key Vault operations 0.0002. Monday evening, about 8:56pm UK time, the total was 10.97: SQL 7.93 (compute 7.80 and storage 0.13) and the plan 2.95. The move from that reading to 14.35 is 3.38.

The desk, the staging slot, and the match API are on the App Service plan, so their own rows are 0.00. The plan is the charge that continues until the resource group is deleted. Until about 9:11pm UK time on Monday the database stayed awake: `/health` ran the match query every minute, and two availability tests called the Match page and `POST /match` every 15 minutes, so the 60 minute auto-pause never started. Those probes were then turned off in the portal. This 14.35 reading still includes that awake stretch, and the lagged Monday usage. Storage remains while the database exists. The first request after a pause waits up to 90 seconds for the database to resume.

P0v3 is the smallest Premium v3 size. Slots start at Standard. Premium v3 is the current generation, and this plan has to host Framework 4.8 and the .NET 10 service together. The slot has no separate charge. It shares the one core.