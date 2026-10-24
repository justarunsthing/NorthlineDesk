# Match rule

INV-1001 has twelve callouts at £102.00. Six were received.

The match service approves £612.00 and leaves 6 unmatched. The stored procedure approves £1,224.00 and leaves 0 unmatched. That second pair is the defect. Keep it until a person swaps the slot and migration 002 drops the procedure.

A shadow row may promote only when the difference is exactly that pair and the ticket is `partial-receipt`. Any other difference stays blocked, including a one-penny drift with the same ticket.

The money formula lives in `MatchCalculator`. The desk calls it. The desk does not reimplement it.