# MCP boundary flow

An agent host starts `Northline.Mcp` and keeps the pipes. The server reads standard input and writes standard output. It uses the in-memory estate in `CalloutEstate`. It has no SQL connection.

The desk may read `dbo.Approval` and show John Doe. These tools cannot.

## A match call, then a SQL call

```mermaid
sequenceDiagram
    participant Host as Agent host
    participant Program as Program.cs
    participant Tools as BoundaryTools
    participant Calc as MatchCalculator

    Host->>Program: start the process
    Host->>Program: initialize
    Program-->>Host: server name northline-match
    Host->>Program: tools/list
    Program-->>Host: match_invoice, describe_purchase_order
    Host->>Program: tools/call match_invoice
    Program->>Tools: Call("match_invoice")
    Tools->>Calc: Decide INV-1001
    Calc-->>Tools: £612.00, 6 unmatched
    Tools-->>Program: Ok
    Program-->>Host: isError false
    Host->>Program: tools/call query, argument sql
    Program->>Tools: Call("query")
    Tools-->>Program: Refuse, no SQL tool
    Program-->>Host: isError true
```

`describe_purchase_order` takes the same path as far as `BoundaryTools`. Its reply names Heston Mechanical, the Reading depot, and 6 received callouts. The reply does not contain John Doe or REQ-1001.

`BoundaryTools` checks the tool name and every argument for `approval`, `requisition`, `vat`, or `sql`. The `Call("query")` arrow is that check. `MatchCalculator` does not run, and the host receives `isError true`.

Closing the input pipe ends the process. The server does not stay listening on a port.