# Legacy match flow

The match page calls `dbo.usp_MatchInvoice`. For INV-1001 the procedure reads the twelve invoice lines, multiplies each quantity by £102.00, and returns the sum. Unmatched is always 0. The receipt is not part of this conversation.

```mermaid
sequenceDiagram
    actor Clerk
    participant Desk as Match page
    participant Procedure as usp_MatchInvoice
    participant Invoice as Invoice and InvoiceLine

    Clerk->>Desk: Open INV-1001
    Desk->>Procedure: EXEC usp_MatchInvoice N'INV-1001'
    Procedure->>Invoice: Read the 12 invoiced lines
    Invoice-->>Procedure: 12 lines at 102.00
    Note over Procedure: Receipt and ReceiptLine are not read
    Procedure->>Procedure: Sum quantity times price = 1224.00
    Procedure->>Procedure: Set unmatched to 0
    Procedure-->>Desk: Approved 1224.00, unmatched 0
    Desk-->>Clerk: Show £1,224.00 and 0 unmatched callouts