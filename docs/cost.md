# Cost to serve

Cost analysis was opened on 25 October 2026. The billing catalog had not indexed the new subscription yet, so the table below stays empty.

Fill this from Cost Management for resource group `rg-northlinedesk-uks` after the plan has been running. Do not type a figure you have not exported.

| Field | Value |
| --- | --- |
| Date range | |
| Currency | |
| Total | |
| App Service plan | |
| Azure SQL | |
| Other meters | |

The plan is the meter that continues until the resource group is deleted. SQL General Purpose serverless can pause after 60 idle minutes, which is the shortest delay Azure allows. While the group is up, `/health` and the availability tests query SQL, so that pause does not get a quiet hour. One test GETs the Match page. The other POSTs INV-1001 to `/match`. Copy both charges from the export. Do not invent them.

P0v3 is the smallest Premium v3 size. Slots start at Standard. Premium v3 is the current generation, and this plan has to host Framework 4.8 and the .NET 10 service together. The slot has no separate charge. It shares the one core.