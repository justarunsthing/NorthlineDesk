# Gatehouse

A normal run scans the repo and exits 0 when it finds nothing. A fixture run scans only the bad samples and exits 1. Those samples are how the red gate is demonstrated.

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Gate as Gatehouse
    participant Repo as src, tests, tools, db

    Dev->>+Gate: Run with no arguments
    Gate->>+Repo: Scan tests, packages, SQL, and the agent folders
    Repo-->>-Gate: No rejected change
    Gate-->>-Dev: Gatehouse found nothing. Exit 0