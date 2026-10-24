# Agent boundary

The versioned copy is `harness/AGENTS.md`. Read that before changing this repo.

You may change the match service, its tests, and app setting values in `infra/main.bicep`.

You may not change the VAT rate, read or write `dbo.Approval` or `dbo.Requisition`, drop an object, or swap the deployment slot. You may not add a package that is not in `harness/package-allowlist.txt`. A test whose only assertion is not-null is rejected. Do not invent numbers in `baseline/`.