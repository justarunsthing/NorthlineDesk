-- Records why £1,224.00 and £612.00 are allowed to differ.
-- This is not a migration. The promote workflow applies it after a person approves the swap.
IF NOT EXISTS (SELECT 1 FROM dbo.MatchExplanation WHERE InvoiceNumber = N'INV-1001')
INSERT INTO dbo.MatchExplanation (InvoiceNumber, Ticket, Note)
VALUES (
    N'INV-1001',
    N'partial-receipt',
    N'6 of 12 callouts were received. Approved net is £612.00. The procedure approved the invoiced £1,224.00 because it does not read the receipt.'
);