# Northline desk

Northline maintains a depot desk for callout work. A supplier does the work, the depot records what arrived, and a clerk matches the supplier's invoice to the purchase order before it is approved. The screen is a .NET Framework Web Forms application. The match itself is a stored procedure, `dbo.usp_MatchInvoice`.

This repository keeps that screen and replaces the decision behind it. The invoice in the story is INV-1001.

## The problem

PO-1001 orders 12 callouts from Heston Mechanical at the Reading depot, at £102.00 net each. REC-1001 records that callouts 1 to 6 arrived. INV-1001 bills all 12.

The received net is £612.00, and 6 callouts have no receipt. The desk approved £1,224.00 and reported nothing unmatched.

| Path | Approved net | Unmatched |
| --- | ---: | ---: |
| `dbo.usp_MatchInvoice` | £1,224.00 | 0 |
| What was received | £612.00 | 6 |

The procedure only reads the invoice. It sums `QuantityInvoiced * UnitPrice` and then sets unmatched to 0 itself. It never reads `dbo.ReceiptLine`. That rule was enough when the invoice and the receipt described the same callouts. On a partial receipt it approves work that has not arrived. The overpayment on this invoice is £612.00.

John Doe approved PO-1001. That approval is a human record on the purchase-order page. It is not part of the match arithmetic. VAT is 20 percent, and both money figures are net.

## The approach

The desk stays. The procedure stays in the database until production has stopped calling it. A .NET 10 match service calculates the new decision beside the old one.

Both answers are written to `dbo.MatchShadow`. Promotion is allowed only for the known difference on INV-1001, and only after a person has recorded why. The cutover is a deployment-slot swap on one Windows plan in UK South. Production starts by calling the procedure. Staging starts by calling the service. The swap carries `MatchMode` and leaves the SQL connection where it is. Migration 002 drops the procedure after that swap.

An agent may change the match service, its tests, and the app settings. An agent may not read the approval, the requisition, or the VAT rate, may not drop an object, and may not swap the slot. Gatehouse rejects a shallow test, a package off the allow list, a drop outside migration 002, and a write to those human tables. The MCP server exposes `match_invoice` and `describe_purchase_order`. It has no SQL tool, and the purchase-order tool does not return John Doe.

## The decision

The service approves `min(invoiced, received) * UnitPrice` on each line. A line is unmatched when the received quantity is below the invoiced quantity. For INV-1001 that is £612.00 and 6 unmatched callouts.

`ShadowGate` accepts a disagreement only when all five values match:

| | Procedure | Service |
| --- | ---: | ---: |
| Approved net | £1,224.00 | £612.00 |
| Unmatched | 0 | 6 |
| Ticket | `partial-receipt` | |

The same numbers with no ticket stay blocked. £1,224.00 against £612.01 stays blocked even with that ticket. A one-penny difference is a different defect. When both sides already agree, no ticket is required.

The ticket is a row in `dbo.MatchExplanation`. A person inserts it at promotion time. It is not part of migration 001, so the first deploy does not mark the difference as understood.

## How it was resolved

`MatchCalculator` holds the formula. The match API exposes it. The Web Forms match page does not multiply anything. It reads `MatchMode` and calls either `dbo.usp_MatchInvoice` or `POST /match`. The purchase-order page still shows John Doe.

Locally, `Cutover_story_against_sql` applies migration 001, sees £1,224.00 against £612.00, blocks the shadow row, records `partial-receipt`, allows the next shadow row, drops the procedure, and restores version 1. The migrator is the same sequence as a command: `--through`, `--explain`, `--assert-retired`, and `--reset-to 1`.

GitHub Actions runs those tests on every push, with SQL Server on the runner. A separate manual workflow scans `gate/fixtures` and finishes red on purpose. Deploy, promote, and reset are a third manual workflow. Deploy uses the `staging` environment and applies migration 001. Promote and reset use the `production` environment, which requires a reviewer. Promote records the ticket, swaps the slot, and only then drops the procedure. Reset puts the procedure and `MatchMode` back.

The Azure account, the resource group, and the federated credential are the one-time setup in `docs/azure-setup.md`. The live slot swap waits on that setup. The local story does not.

```bash
dotnet test
```

That covers the money, the gate, and the agent boundary. To run the cutover against SQL:

```bash
docker compose up -d
export NORTHLINE_SQL='Server=localhost,1433;Database=master;User ID=sa;Password=Northline_Local_Only_123;Encrypt=True;TrustServerCertificate=True'
dotnet test --filter Cutover_story_against_sql
```

The password is for this container only. The desk targets .NET Framework 4.8 and is not in `NorthlineDesk.sln`. That solution is what `dotnet test` builds.

## Conclusion

The clerk's screen is the same screen. The decision behind INV-1001 changed from the invoiced £1,224.00 to the received £612.00, with 6 callouts left unmatched. The procedure remains available until a person has accepted that exact difference and the slot has swapped. After the swap, migration 002 removes it. The tests lock the figures. The harness bounds what an agent may change. The approval stays on the desk, with John Doe, where a person can see it and a tool cannot.

## Read it in this order

The diagrams are in `db/estate.md`, `spec/legacy-flow.md`, `spec/shadow-flow.md`, `spec/slot-flow.md`, and `docs/cutover.md`. GitHub renders them on the file page.

1. `spec/match-invoice.md` — the support complaint and the numbers a test has to assert.
2. `db/migrations/001_estate.sql` — the tables, the seed, and the procedure that returns £1,224.00.
3. `src/Northline.Match/MatchCalculator.cs` and `ShadowGate.cs` — the £612.00 decision, and the only difference promotion may accept.
4. `tests/Northline.Tests` — the figures, the one-penny drift, the procedure text, the gate, and the MCP refusal.
5. `src/Northline.MatchApi/Program.cs` — `POST /match` and `POST /shadow/{invoice}`.
6. `src/NorthlineDesk/Match.aspx.cs` — the desk calls the procedure or the service. It does not calculate the money.
7. `src/NorthlineDesk/PurchaseOrder.aspx.cs` — twelve lines, six received, and John Doe.
8. `tools/Northline.Gatehouse` and `gate/fixtures` — what a bad generated change looks like.
9. `harness/` — the rule, the skill, the package list, and the MCP server.
10. `infra/main.bicep` and `docs/cutover.md` — one plan, the slot, and the setting a swap carries.
11. `.github/workflows/deploy.yml` and `scripts/azure.sh` — deploy, then a person promotes.
12. `docs/azure-setup.md` and `docs/cost.md` — the one-time account setup. The cost table stays blank until a deploy is exported.