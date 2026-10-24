# Changelog

One bullet per commit.

The clerk is the person on the Northline desk. The match page is the screen they will build later. The procedure and the invoice tables are already in `001_estate.sql`. Twelve lines at £102.00 produce £1,224.00. The procedure then sets unmatched to 0 itself, so callouts 7 to 12 never appear as a shortage.

The first diagram is the new process. The second is the comparison. The in-memory tests never enter either diagram. They call `MatchCalculator` and `ShadowGate` directly.

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