# MIG-28 — Cutover Checklist & Legacy Freeze

> Phase 7 gate 1. Records the cutover from the legacy .NET Framework 4.5.1 stack
> (WCF SOAP + EF6/SQL Server + Silverlight) to the migrated stack
> (Aspire + Clean Architecture + Dapper/PostgreSQL + Blazor), and freezes `Legacy/`.

## 1. Parity verification — ✅ DONE

- Automated parity suite: `tests/MusicStore.IntegrationTests/LegacyParityTests.cs` (MIG-27)
- Report: `docs/MIG-27-legacy-parity.md` — `GetArtist()`/`GetGenres()` payloads match
  `GET /catalog/artists` / `GET /catalog/genres` over the shared seed data
- Result: **parity verified**; sign-off recorded in the report

## 2. Final data snapshot — ✅ DONE

The legacy SQL Server data was migrated to PostgreSQL in MIG-08 (schema parity) and
MIG-09 (data migration). The final state is:

- Schema: `db/001_create_tables.sql` (verified by `db/200_verify_schema_parity.sql`)
- Seed/reference data: `db/100_seed.sql`
- Migrations tooling: `src/MusicStore.DbMigrations` (DbUp, journal table `scriptsrun`)

The legacy SQL Server database is **read-only from here on**; no further data flows
into it. The PostgreSQL database is the single source of truth.

## 3. Legacy freeze tag — ✅ DONE

- Tag: `legacy/final` on the last commit containing the full `Legacy/` tree
- From this commit on, `Legacy/` is frozen: no further changes (MIG-29 removes it
  from the working tree; the tag preserves the exact final state for audit)

## 4. Cutover checklist

| # | Item | Status |
|---|---|---|
| 1 | Parity report signed off (MIG-27) | ✅ |
| 2 | PostgreSQL schema parity verified (MIG-08) | ✅ |
| 3 | Data migrated / seed verified (MIG-09) | ✅ |
| 4 | REST APIs replace WCF (MIG-17–MIG-21) | ✅ |
| 5 | YARP gateway is the single front door (MIG-22) | ✅ |
| 6 | Blazor UI replaces Silverlight (MIG-31–MIG-34) | ✅ |
| 7 | End-to-end test suites green (30/30 integration, 47/47 unit, 39/39 repo) | ✅ |
| 8 | Legacy tree frozen + tagged `legacy/final` | ✅ |
| 9 | Legacy projects removed from solution (MIG-29) | → next task |
| 10 | Final docs (MIG-30) | → next task |

## 5. Rollback plan

If a critical defect is found post-cutover:

1. The legacy stack remains runnable from tag `legacy/final` (IIS + WCF + SQL Server).
2. Data written to PostgreSQL after cutover would need re-export if rollback is required.
3. The gateway routes (`/catalog/**`, `/songs/**`) are the only public contract; clients
   never talk to the APIs directly, so a UI/API rollback does not affect consumers.
