# Match invoice INV-1001

Support complaint, Northline depot, Reading.

Heston Mechanical were called out on purchase order PO-1001. The order was for 12 callouts at £102.00 net each. The depot received 6, recorded on receipt REC-1001 as callouts 1 to 6. Invoice INV-1001 billed all 12. The screen approved the whole invoice.

## Documents

| Document | Number | What it holds |
| --- | --- | --- |
| Purchase order | PO-1001 | 12 callouts, £102.00 net each |
| Receipt | REC-1001 | Callouts 1 to 6 received, one each |
| Invoice | INV-1001 | All 12 callouts invoiced, one each, at £102.00 |
| Approval | — | John Doe approved the purchase order on 28 September 2026. The match does not read this. |
| VAT rate | — | 20%, in force since 4 January 2011. The match does not use it. |

12 × £102.00 = £1,224.00 invoiced.

6 × £102.00 = £612.00 received.

The figures are exact. A one-penny drift is a different defect.

## The defect

The live rule sums the invoice lines and never reads the receipt. Unmatched is written as 0. For INV-1001 it approves £1,224.00 and reports 0 unmatched callouts. The receipt is already stored. The rule does not consult it.

## The decision that replaces it

The match approves only the quantity that was both invoiced and received.

- Approved net: £612.00
- Unmatched callouts: 6 (callouts 7 to 12)

VAT stays out of this comparison. Both paths are net.

## What a test has to assert

A test that checks the type, or that the result is not null, does not finish this complaint. The assertions are the two numbers: approved net £612.00, and 6 unmatched callouts. The legacy rule, while it still exists, asserts the opposite pair: £1,224.00 and 0 unmatched. That pair is the defect, kept until the switch, then removed.