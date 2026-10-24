# Service match and shadow comparison

The service match reads the invoice and the receipt. The shadow comparison runs that match beside the stored procedure and records both answers.

```mermaid
sequenceDiagram
    participant Reader as EstateDatabase
    participant Invoice as Invoice and InvoiceLine
    participant Receipt as ReceiptLine
    participant Calculator as MatchCalculator

    Reader->>Invoice: Read the 12 invoiced lines for INV-1001
    Reader->>Receipt: Sum the quantity received for each order line
    Receipt-->>Reader: Callouts 1 to 6 are 1. Callouts 7 to 12 are 0
    Reader->>Calculator: Decide with those 12 lines
    Calculator-->>Reader: 612.00 approved, 6 unmatched, match-service