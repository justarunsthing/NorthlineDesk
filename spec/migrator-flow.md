# Migrator flow

`tools/Northline.Migrator` applies the scripts by calling `MigrationRunner`. The container for this rehearsal starts on version 1, so `dbo.usp_MatchInvoice` is present.

## Rehearse on the local container

```mermaid
sequenceDiagram
    participant You as Operator
    participant Mig as Northline.Migrator
    participant Runner as MigrationRunner
    participant SQL as SQL Server

    You->>Mig: --through 1
    Mig->>Runner: ApplyThroughAsync 1
    Runner->>SQL: migration 001 already recorded
    Mig-->>You: Applied migrations through 1
    You->>Mig: --explain
    Mig->>Runner: ApplyExplanationAsync
    Runner->>SQL: insert partial-receipt for INV-1001
    Mig-->>You: Recorded ticket partial-receipt
    You->>Mig: --through 2 --assert-retired
    Mig->>Runner: ApplyThroughAsync 2
    Runner->>SQL: DROP PROCEDURE usp_MatchInvoice
    Mig->>Runner: ProcedureObjectIdAsync
    Runner-->>Mig: null
    Mig-->>You: OBJECT_ID is null
    You->>Mig: --reset-to 1
    Mig->>Runner: ResetToVersionAsync 1
    Runner->>SQL: restore the procedure, clear shadow rows and the ticket
    Mig-->>You: Reset to version 1
```

`--through 1` is what a deploy applies. The procedure stays. `--explain` followed by `--through 2 --assert-retired` is what a promotion applies. `--reset-to 1` is only the rehearsal. A real promotion does not put the procedure back.

The promote job posts `/shadow/INV-1001` and continues only when `promotionBlocked` is false, before it calls `--through 2`.