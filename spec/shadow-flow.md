# Service match

The service match reads the invoice and the receipt. The shadow comparison, including the procedure and the ticket, is the promote diagram in `docs/cutover.md`.

```mermaid
sequenceDiagram
    participant Reader as EstateDatabase
    participant Invoice as InvoiceLine
    participant Receipt as ReceiptLine
    participant Calculator as MatchCalculator

    Reader->>+Invoice: Read the 12 invoiced lines for INV-1001
    Invoice-->>-Reader: 12 lines at 102.00
    Reader->>+Receipt: Sum the quantity received for each order line
    Receipt-->>-Reader: 1 for callouts 1 to 6, otherwise 0
    Reader->>+Calculator: Decide with those 12 lines
    Calculator-->>-Reader: 612.00 approved, 6 unmatched, match-service
```