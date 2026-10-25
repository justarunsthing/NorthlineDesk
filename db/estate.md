# Northline estate

The match reads the supplier, order, receipt, and invoice tables.
Approval, requisition, and VAT are human records. The match service does not read them.
MatchExplanation and MatchShadow store an invoice number as text. They have no foreign key to Invoice.

```mermaid
erDiagram
    Supplier ||--o{ PurchaseOrder : supplies
    PurchaseOrder ||--o{ PurchaseOrderLine : contains
    PurchaseOrder ||--o{ Receipt : received_as
    PurchaseOrder ||--o{ Invoice : billed_as
    PurchaseOrder ||--o{ Approval : approved_by
    PurchaseOrder ||--o{ Requisition : requested_by
    Receipt ||--o{ ReceiptLine : contains
    PurchaseOrderLine ||--o{ ReceiptLine : received_on
    Invoice ||--o{ InvoiceLine : contains
    PurchaseOrderLine ||--o{ InvoiceLine : billed_on

    VatRate {
        int VatRateId PK
        decimal Rate
        date EffectiveFrom
    }
    MatchExplanation {
        nvarchar InvoiceNumber PK
        nvarchar Ticket
        nvarchar Note
    }
    MatchShadow {
        int MatchShadowId PK
        nvarchar InvoiceNumber
        decimal LegacyApproved
        decimal ServiceApproved
        int LegacyUnmatched
        int ServiceUnmatched
        nvarchar ExplainedByTicket
    }
```