-- Retire the legacy decision.
-- Apply only after production has swapped onto the match service
-- and the shadow difference for INV-1001 is explained by partial-receipt.
DROP PROCEDURE IF EXISTS dbo.usp_MatchInvoice;