# Match an invoice

Use this when changing how a Northline invoice is approved.

1. Read `spec/match-invoice.md`. The test asserts £612.00 approved and 6 unmatched callouts.
2. Change `MatchCalculator` or the SQL the service reads. Do not change the procedure to hide the defect. The procedure is retired by migration 002 after the swap.
3. Do not open `dbo.Approval`, `dbo.Requisition`, or `dbo.VatRate`.
4. Run `dotnet test`. Then run `dotnet run --project tools/Northline.Gatehouse`.
5. If you measured the session, fill `baseline/manual.json` or `baseline/agent.json`. Leave the numbers null if you did not measure it.