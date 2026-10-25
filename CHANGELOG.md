# Changelog

One bullet per commit.

## Unreleased

### Added

- 24 October 2026. Repository skeleton and the support complaint for INV-1001. The tests have to assert £612.00 and 6 unmatched callouts.
- Estate migration and diagram: supplier, purchase order, receipt, and invoice tables, the PO-1001 seed, `dbo.usp_MatchInvoice`, and `db/estate.md`.
- Retirement migration that drops `dbo.usp_MatchInvoice` after the cutover.
- `partial-receipt` explanation for INV-1001, applied at promotion rather than during migration.
- Match calculator that approves the received quantity, with tests for the £612.00 partial receipt.
- Sequence diagram of the legacy match for INV-1001.
- Shadow gate that blocks promotion unless the INV-1001 difference is the explained partial receipt.
- Data project that applies the migrations, reads INV-1001, calls the procedure and the match calculator, and writes the shadow row.
- Sequence diagrams for the service match and the shadow comparison.
- Shadow sequence diagrams, so each reply returns to the participant that was called.
- Match service. POST /match returns the in-memory decision for INV-1001. Shadow comparison waits for SQL.
- Web Forms desk with the Reading depot page, the match page, and the purchase order that shows John Doe.
- Gatehouse scan for a shallow test, a package off the allow list, a drop outside migration 002, and a write to the human tables.
- Sequence diagram for a clean Gatehouse run and for the fixture run that must fail.
- Fenced MCP server `northline-match` with `match_invoice` and `describe_purchase_order`. It answers £612.00 and 6 unmatched callouts, and it refuses SQL. The purchase-order tool does not return John Doe.
- Sequence diagram of the MCP host: `match_invoice` returns £612.00, and a SQL call is refused before the calculator runs.
- Agent boundary in `AGENTS.md` and `harness/AGENTS.md`, with the match rule and the match-invoice skill. `baseline/manual.json` and `baseline/agent.json` stay unmeasured.
- Sequence diagram of a host following the harness: read the boundary, edit `MatchCalculator`, run Gatehouse, and leave the baseline unmeasured.
- Local SQL Server in `docker-compose.yml`. File tests lock the procedure text, migration 002, and the ticket script. `Cutover_story_against_sql` runs that cutover against the container and restores version 1.
- Migrator command. `--through` applies migrations, `--explain` records ticket `partial-receipt`, `--assert-retired` checks the procedure is gone, and `--reset-to 1` restores version 1.
- Sequence diagram of the migrator rehearsal: apply through 1, record `partial-receipt`, drop the procedure, and reset to version 1.
- Azure template for one UK South Windows plan, the desk, the staging slot, the match API, SQL, and Key Vault. `MatchMode` swaps. The availability tests expect HTTP 200 and `612.00`. The budget stays off until an email is set.
- Diagram of the UK South plan: production starts on the procedure, staging starts on the match service, and a swap carries `MatchMode` only.
- CI workflow. Every push runs the tests against SQL Server and requires Gatehouse to be clean.
- Demo red-gate workflow. A manual run rejects the three bad fixtures and finishes red on purpose.
- Deploy workflow. `deploy` publishes the desk and the match API, applies the Bicep file, and runs migration 001 on the staging environment. `promote` and `reset` run on the production environment.
- The red Deploy runs, and the change that made each one pass, are in `docs/deploy-notes.md`.
- Azure and GitHub setup checklist. The signup, the resource group, and the federated credential stay until the first deploy.
- Cutover note. Promote records `partial-receipt`, swaps `MatchMode`, then drops the procedure. Reset puts that state back and also waits for the production reviewer.
- Cost page. The table stays empty until Cost Management for `rg-northlinedesk-uks` has been exported.
- Added README.md