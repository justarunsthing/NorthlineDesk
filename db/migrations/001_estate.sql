-- Estate for the NorthlineDesk.
-- 12 callouts at 102.00. Callouts 1 to 6 are received. The invoice bills all 12.

IF OBJECT_ID(N'dbo.Supplier', N'U') IS NULL
CREATE TABLE dbo.Supplier (
    SupplierId int NOT NULL CONSTRAINT PK_Supplier PRIMARY KEY,
    Name nvarchar(120) NOT NULL
);

IF OBJECT_ID(N'dbo.PurchaseOrder', N'U') IS NULL
CREATE TABLE dbo.PurchaseOrder (
    PurchaseOrderId int NOT NULL CONSTRAINT PK_PurchaseOrder PRIMARY KEY,
    Number nvarchar(32) NOT NULL CONSTRAINT UQ_PurchaseOrder_Number UNIQUE,
    SupplierId int NOT NULL,
    Depot nvarchar(120) NOT NULL,
    OrderedOn date NOT NULL,
    CONSTRAINT FK_PurchaseOrder_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier (SupplierId)
);

IF OBJECT_ID(N'dbo.PurchaseOrderLine', N'U') IS NULL
CREATE TABLE dbo.PurchaseOrderLine (
    PurchaseOrderLineId int NOT NULL CONSTRAINT PK_PurchaseOrderLine PRIMARY KEY,
    PurchaseOrderId int NOT NULL,
    LineNumber int NOT NULL,
    Description nvarchar(80) NOT NULL,
    QuantityOrdered decimal(18, 2) NOT NULL,
    UnitPrice decimal(18, 2) NOT NULL,
    CONSTRAINT FK_PurchaseOrderLine_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder (PurchaseOrderId),
    CONSTRAINT UQ_PurchaseOrderLine UNIQUE (PurchaseOrderId, LineNumber)
);

IF OBJECT_ID(N'dbo.Receipt', N'U') IS NULL
CREATE TABLE dbo.Receipt (
    ReceiptId int NOT NULL CONSTRAINT PK_Receipt PRIMARY KEY,
    Number nvarchar(32) NOT NULL CONSTRAINT UQ_Receipt_Number UNIQUE,
    PurchaseOrderId int NOT NULL,
    ReceivedOn date NOT NULL,
    CONSTRAINT FK_Receipt_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder (PurchaseOrderId)
);

IF OBJECT_ID(N'dbo.ReceiptLine', N'U') IS NULL
CREATE TABLE dbo.ReceiptLine (
    ReceiptLineId int NOT NULL CONSTRAINT PK_ReceiptLine PRIMARY KEY,
    ReceiptId int NOT NULL,
    PurchaseOrderLineId int NOT NULL,
    QuantityReceived decimal(18, 2) NOT NULL,
    CONSTRAINT FK_ReceiptLine_Receipt FOREIGN KEY (ReceiptId) REFERENCES dbo.Receipt (ReceiptId),
    CONSTRAINT FK_ReceiptLine_PurchaseOrderLine FOREIGN KEY (PurchaseOrderLineId) REFERENCES dbo.PurchaseOrderLine (PurchaseOrderLineId)
);

IF OBJECT_ID(N'dbo.Invoice', N'U') IS NULL
CREATE TABLE dbo.Invoice (
    InvoiceId int NOT NULL CONSTRAINT PK_Invoice PRIMARY KEY,
    Number nvarchar(32) NOT NULL CONSTRAINT UQ_Invoice_Number UNIQUE,
    PurchaseOrderId int NOT NULL,
    InvoicedOn date NOT NULL,
    CONSTRAINT FK_Invoice_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder (PurchaseOrderId)
);

IF OBJECT_ID(N'dbo.InvoiceLine', N'U') IS NULL
CREATE TABLE dbo.InvoiceLine (
    InvoiceLineId int NOT NULL CONSTRAINT PK_InvoiceLine PRIMARY KEY,
    InvoiceId int NOT NULL,
    PurchaseOrderLineId int NOT NULL,
    QuantityInvoiced decimal(18, 2) NOT NULL,
    UnitPrice decimal(18, 2) NOT NULL,
    CONSTRAINT FK_InvoiceLine_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice (InvoiceId),
    CONSTRAINT FK_InvoiceLine_PurchaseOrderLine FOREIGN KEY (PurchaseOrderLineId) REFERENCES dbo.PurchaseOrderLine (PurchaseOrderLineId)
);

-- Human records. The match service, the migrator, and the MCP tools do not read these.
IF OBJECT_ID(N'dbo.VatRate', N'U') IS NULL
CREATE TABLE dbo.VatRate (
    VatRateId int NOT NULL CONSTRAINT PK_VatRate PRIMARY KEY,
    Rate decimal(6, 4) NOT NULL,
    EffectiveFrom date NOT NULL
);

IF OBJECT_ID(N'dbo.Approval', N'U') IS NULL
CREATE TABLE dbo.Approval (
    ApprovalId int NOT NULL CONSTRAINT PK_Approval PRIMARY KEY,
    PurchaseOrderId int NOT NULL,
    Approver nvarchar(80) NOT NULL,
    ApprovedOn date NOT NULL,
    CONSTRAINT FK_Approval_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder (PurchaseOrderId)
);

IF OBJECT_ID(N'dbo.Requisition', N'U') IS NULL
CREATE TABLE dbo.Requisition (
    RequisitionId int NOT NULL CONSTRAINT PK_Requisition PRIMARY KEY,
    Number nvarchar(32) NOT NULL,
    PurchaseOrderId int NOT NULL,
    RequestedBy nvarchar(80) NOT NULL,
    CONSTRAINT FK_Requisition_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder (PurchaseOrderId)
);

IF OBJECT_ID(N'dbo.MatchExplanation', N'U') IS NULL
CREATE TABLE dbo.MatchExplanation (
    InvoiceNumber nvarchar(32) NOT NULL CONSTRAINT PK_MatchExplanation PRIMARY KEY,
    Ticket nvarchar(64) NOT NULL,
    Note nvarchar(400) NOT NULL
);

IF OBJECT_ID(N'dbo.MatchShadow', N'U') IS NULL
CREATE TABLE dbo.MatchShadow (
    MatchShadowId int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_MatchShadow PRIMARY KEY,
    InvoiceNumber nvarchar(32) NOT NULL,
    LegacyApproved decimal(18, 2) NOT NULL,
    ServiceApproved decimal(18, 2) NOT NULL,
    LegacyUnmatched int NOT NULL,
    ServiceUnmatched int NOT NULL,
    ExplainedByTicket nvarchar(64) NULL,
    RecordedAt datetime2 NOT NULL CONSTRAINT DF_MatchShadow_RecordedAt DEFAULT SYSUTCDATETIME()
);

IF NOT EXISTS (SELECT 1 FROM dbo.Supplier WHERE SupplierId = 1)
    INSERT INTO dbo.Supplier (SupplierId, Name) VALUES (1, N'Heston Mechanical');

IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrder WHERE PurchaseOrderId = 1)
    INSERT INTO dbo.PurchaseOrder (PurchaseOrderId, Number, SupplierId, Depot, OrderedOn)
    VALUES (1, N'PO-1001', 1, N'Northline depot, Reading', '20260928');

IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrderLine WHERE PurchaseOrderLineId = 1)
INSERT INTO dbo.PurchaseOrderLine (PurchaseOrderLineId, PurchaseOrderId, LineNumber, Description, QuantityOrdered, UnitPrice) VALUES
    (1,  1,  1,  N'Callout 1',  1, 102.00),
    (2,  1,  2,  N'Callout 2',  1, 102.00),
    (3,  1,  3,  N'Callout 3',  1, 102.00),
    (4,  1,  4,  N'Callout 4',  1, 102.00),
    (5,  1,  5,  N'Callout 5',  1, 102.00),
    (6,  1,  6,  N'Callout 6',  1, 102.00),
    (7,  1,  7,  N'Callout 7',  1, 102.00),
    (8,  1,  8,  N'Callout 8',  1, 102.00),
    (9,  1,  9,  N'Callout 9',  1, 102.00),
    (10, 1, 10, N'Callout 10', 1, 102.00),
    (11, 1, 11, N'Callout 11', 1, 102.00),
    (12, 1, 12, N'Callout 12', 1, 102.00);

IF NOT EXISTS (SELECT 1 FROM dbo.Receipt WHERE ReceiptId = 1)
    INSERT INTO dbo.Receipt (ReceiptId, Number, PurchaseOrderId, ReceivedOn)
    VALUES (1, N'REC-1001', 1, '20261002');

IF NOT EXISTS (SELECT 1 FROM dbo.ReceiptLine WHERE ReceiptLineId = 1)
INSERT INTO dbo.ReceiptLine (ReceiptLineId, ReceiptId, PurchaseOrderLineId, QuantityReceived) VALUES
    (1, 1, 1, 1),
    (2, 1, 2, 1),
    (3, 1, 3, 1),
    (4, 1, 4, 1),
    (5, 1, 5, 1),
    (6, 1, 6, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId = 1)
    INSERT INTO dbo.Invoice (InvoiceId, Number, PurchaseOrderId, InvoicedOn)
    VALUES (1, N'INV-1001', 1, '20261016');

IF NOT EXISTS (SELECT 1 FROM dbo.InvoiceLine WHERE InvoiceLineId = 1)
INSERT INTO dbo.InvoiceLine (InvoiceLineId, InvoiceId, PurchaseOrderLineId, QuantityInvoiced, UnitPrice) VALUES
    (1,  1, 1,  1, 102.00),
    (2,  1, 2,  1, 102.00),
    (3,  1, 3,  1, 102.00),
    (4,  1, 4,  1, 102.00),
    (5,  1, 5,  1, 102.00),
    (6,  1, 6,  1, 102.00),
    (7,  1, 7,  1, 102.00),
    (8,  1, 8,  1, 102.00),
    (9,  1, 9,  1, 102.00),
    (10, 1, 10, 1, 102.00),
    (11, 1, 11, 1, 102.00),
    (12, 1, 12, 1, 102.00);

IF NOT EXISTS (SELECT 1 FROM dbo.VatRate WHERE VatRateId = 1)
    INSERT INTO dbo.VatRate (VatRateId, Rate, EffectiveFrom) VALUES (1, 0.2000, '20110104');

IF NOT EXISTS (SELECT 1 FROM dbo.Approval WHERE ApprovalId = 1)
    INSERT INTO dbo.Approval (ApprovalId, PurchaseOrderId, Approver, ApprovedOn)
    VALUES (1, 1, N'John Doe', '20260928');

IF NOT EXISTS (SELECT 1 FROM dbo.Requisition WHERE RequisitionId = 1)
    INSERT INTO dbo.Requisition (RequisitionId, Number, PurchaseOrderId, RequestedBy)
    VALUES (1, N'REQ-1001', 1, N'A. Khan');

GO

CREATE OR ALTER PROCEDURE dbo.usp_MatchInvoice
    @InvoiceNumber nvarchar(32)
AS
BEGIN
    SET NOCOUNT ON;

    -- Legacy decision. It sums the invoice. It does not read what was received.
    -- INV-1001 therefore approves 1224.00 and reports 0 unmatched callouts.
    SELECT
        i.Number AS InvoiceNumber,
        CAST(SUM(il.QuantityInvoiced * il.UnitPrice) AS decimal(18, 2)) AS Approved,
        CAST(0 AS int) AS Unmatched
    FROM dbo.Invoice AS i
    INNER JOIN dbo.InvoiceLine AS il ON il.InvoiceId = i.InvoiceId
    WHERE i.Number = @InvoiceNumber
    GROUP BY i.Number;
END