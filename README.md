# MusicStore

A music catalog application, migrated from legacy .NET Framework 4.5.1 (WCF SOAP +
EF6/SQL Server + Silverlight) to **.NET 10 + .NET Aspire** with Clean Architecture,
Dapper + PostgreSQL, a YARP gateway and a Blazor Web App UI.

## Quick start

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com), Docker (for the Aspire-managed
PostgreSQL container).

```bash
dotnet run --project src/MusicStore.AppHost
```

The Aspire dashboard opens and starts every resource:

| Resource | What it is |
|---|---|
| `musicstore-db` | PostgreSQL container (+ pgAdmin) |
| `migrations` | DbUp console that applies `db/*.sql` (schema + seed) once |
| `catalog` | Catalog API (artists, genres, albums) |
| `songs` | Songs microservice |
| `gateway` | YARP gateway — the single API front door |
| `web` | Blazor UI (open its endpoint in the browser) |

## Solution layout

| Folder | Projects |
|---|---|
| `src/` | AppHost, ServiceDefaults, Domain, Application, Infrastructure, Api, Songs.Api, Gateway, Web, DbMigrations |
| `tests/` | Application.UnitTests (47), Infrastructure.Tests (39), IntegrationTests (30) |
| `db/` | SQL schema, seed and parity-verification scripts |
| `docs/` | Migration reports (schema parity, legacy parity, cutover checklist, UI plan) |

## Running the tests

```bash
# Unit tests (no DB)
dotnet test tests/MusicStore.Application.UnitTests

# Repository tests (spins a PostgreSQL Testcontainer; Docker required)
dotnet test tests/MusicStore.Infrastructure.Tests

# End-to-end tests (boots the full Aspire app; Docker required)
dotnet test tests/MusicStore.IntegrationTests
```

## API

The YARP gateway is the single front door:

- `/catalog/**` → catalog API (artists, genres, albums)
- `/songs/**` → songs API
- Errors: RFC 9457 ProblemDetails (`application/problem+json`)
- OpenAPI: `/catalog/openapi/v1.json`, `/songs/openapi/v1.json` (through the gateway)

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full architecture, and
[MIGRATION-TRACKER.md](MIGRATION-TRACKER.md) for the migration history
(36 tasks, 9 phases — completed).

## Legacy

The original .NET Framework 4.5.1 stack (WCF + EF6/SQL Server + Silverlight) was
decommissioned in Phase 7. Its final state is preserved at git tag `legacy/final`
and documented in `docs/MIG-28-cutover-checklist.md`.
