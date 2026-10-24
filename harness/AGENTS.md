# Northline match harness

This folder is the versioned engineering asset: the rule, the skill, the package list, and the MCP server.

## May

- Change `src/Northline.Match`, `src/Northline.MatchApi`, and their tests.
- Change app setting values in `infra/main.bicep`.

## May not

- Change `dbo.VatRate`, or read or write `dbo.Approval` or `dbo.Requisition`. The desk shows the approver. These tools do not.
- Drop a table, view, or procedure. Migration `002_retire_procedure.sql` is the only drop, and a person applies it by promoting.
- Swap the staging slot. The production GitHub Environment has a required reviewer.
- Add a `PackageReference` that is not in `package-allowlist.txt`.
- Treat a not-null assertion as a finished test. `spec/match-invoice.md` names the figures.
- Write token counts, minutes, or rework into `baseline/` unless a session measured them.

## Routing

The spec and `MatchCalculator` hold the money formula. A generated change may edit that formula only by following `spec/match-invoice.md`, and the tests have to assert £612.00 and 6 unmatched callouts.

Tests may be written by hand or by an agent. A test whose only assertion is not-null fails Gatehouse. Record a hand session in `baseline/manual.json` and a harness session in `baseline/agent.json`. Leave the numbers null until the session happened.

A person records ticket `partial-receipt`, approves the production environment, and only then does migration 002 run. The agent does not swap the slot.

## Check

From the repo root:

```bash
dotnet run --project tools/Northline.Gatehouse
dotnet test
```

Gatehouse must be clean before a change is done. The fixtures under `gate/fixtures/` are the known failures: a shallow test, a package off the list, and a drop outside migration 002.