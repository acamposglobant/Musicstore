# MusicStore — Target Architecture

> Migrated stack: .NET 10 + .NET Aspire, Clean Architecture, Dapper + PostgreSQL,
> YARP gateway, Blazor Web App. The legacy .NET Framework 4.5.1 stack (WCF SOAP +
> EF6/SQL Server + Silverlight) was decommissioned in Phase 7 — its final state is
> preserved at git tag `legacy/final`.

## 1. Solution Overview

Solution file: `MusicStore.slnx` — 12 projects:

| Folder | Project | Type | Purpose |
|---|---|---|---|
| src | `MusicStore.AppHost` | Aspire AppHost | Orchestrates the whole system (Postgres, migrations, APIs, gateway, web) |
| src | `MusicStore.ServiceDefaults` | Class Library | OTel tracing/metrics, health endpoints, resilience, service discovery |
| src | `MusicStore.Domain` | Class Library | Domain entities + invariants (no dependencies) |
| src | `MusicStore.Application` | Class Library | Handlers (CQRS-style), DTOs, validation, exceptions |
| src | `MusicStore.Infrastructure` | Class Library | Dapper repositories, Npgsql, UnitOfWork |
| src | `MusicStore.Api` | Minimal API | Catalog service (artists, genres, albums) |
| src | `MusicStore.Songs.Api` | Minimal API | Songs microservice (owns all song operations) |
| src | `MusicStore.Gateway` | YARP gateway | Single front door: `/catalog/**`, `/songs/**` |
| src | `MusicStore.Web` | Blazor Web App | Browser UI (Interactive Server), typed client → gateway |
| src | `MusicStore.DbMigrations` | Console (DbUp) | SQL migrations + seed, journal table |
| tests | `MusicStore.Application.UnitTests` | xUnit | Handler unit tests (NSubstitute mocks, zero DB) |
| tests | `MusicStore.Infrastructure.Tests` | xUnit | Dapper repository tests against real PostgreSQL (Testcontainers) |
| tests | `MusicStore.IntegrationTests` | xUnit | End-to-end Aspire tests: CRUD, parity, ProblemDetails, UI smoke |

## 2. Runtime Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  Browser — Blazor Web App (MusicStore.Web, Interactive Server)│
└──────────────────────────┬───────────────────────────────────┘
                           │ HTTP (typed CatalogApiClient)
┌──────────────────────────▼───────────────────────────────────┐
│  YARP Gateway (single front door)                             │
│  /catalog/** → catalog API   /songs/** → songs API            │
│  service discovery via custom IForwarderHttpClientFactory     │
└───────────────┬───────────────────────────┬──────────────────┘
                │                           │
┌───────────────▼──────────┐  ┌─────────────▼──────────────────┐
│  MusicStore.Api          │  │  MusicStore.Songs.Api          │
│  (artists, genres,       │  │  (songs; GET /songs?albumId=)  │
│   albums)                │  │                                │
│  Minimal APIs + handlers │  │  Minimal APIs + handlers       │
└───────────────┬──────────┘  └─────────────┬──────────────────┘
                │ Dapper (Npgsql)           │ Dapper (Npgsql)
┌───────────────▼───────────────────────────▼──────────────────┐
│  PostgreSQL (Aspire container, DbUp migrations on startup)    │
└──────────────────────────────────────────────────────────────┘
```

- **AppHost** (`src/MusicStore.AppHost/AppHost.cs`): `musicstore-db` (Postgres +
  pgAdmin), `migrations` (DbUp, runs once), `catalog` + `songs` APIs, `gateway`,
  `web` — with `WithReference`/`WaitFor` wiring and service discovery.
- **Gateway**: YARP with a custom `IForwarderHttpClientFactory` that attaches the
  Aspire service-discovery handler to the forwarder pipeline (destinations are
  logical names `http://catalog` / `http://songs`).
- **UI**: Blazor Interactive Server; all HTTP traffic goes through the gateway;
  RFC 9457 ProblemDetails surfaced as user-visible error messages.

## 3. Clean Architecture layers

| Layer | Project | Rules |
|---|---|---|
| Domain | `MusicStore.Domain` | Entities + invariants; zero dependencies |
| Application | `MusicStore.Application` | Handlers per use case, DTOs, `ValidationException`/`NotFoundException`; depends only on Domain |
| Infrastructure | `MusicStore.Infrastructure` | Dapper repositories + Npgsql UnitOfWork; implements Application ports |
| Presentation | `MusicStore.Api`, `MusicStore.Songs.Api`, `MusicStore.Gateway`, `MusicStore.Web` | Minimal APIs / proxy / UI; depends on Application |

Cross-cutting: `MusicStore.ServiceDefaults` (OTel, health, resilience) referenced by
every service.

## 4. Data access (Dapper + PostgreSQL)

- `NpgsqlDataSource` registered via Aspire's `AddNpgsqlDataSource` (connection string
  injected as `ConnectionStrings__musicstore-db-database`)
- snake_case mapping, multi-mapping for enriched reads, `interval` ↔ `TimeSpan`
- `UnitOfWork` (Npgsql transactions) for multi-write operations
- Schema + seed applied by DbUp at startup (`db/*.sql`, journal table `scriptsrun`)

## 5. API surface (via gateway)

| Route | Purpose |
|---|---|
| `GET/POST/PUT/DELETE /catalog/artists[/{id}]` | Artists CRUD |
| `GET/POST/PUT/DELETE /catalog/genres[/{id}]` | Genres CRUD |
| `GET/POST/PUT/DELETE /catalog/albums[/{id}]` | Albums CRUD |
| `GET /catalog/albums/with-details` | Enriched albums (artist/genre names) |
| `GET/POST/PUT/DELETE /songs/songs[/{id}]?albumId=` | Songs CRUD (songs API) |
| `/health`, `/alive` | Health probes on every service |
| Errors | RFC 9457 ProblemDetails (`application/problem+json`), 404/422/500 |

OpenAPI documents (`/openapi/v1.json` on each API) are the contract source of truth.

## 6. Testing strategy

| Suite | Project | What it covers |
|---|---|---|
| Unit | `MusicStore.Application.UnitTests` (47) | Handler logic with mocked repositories |
| Repository | `MusicStore.Infrastructure.Tests` (39) | Real PostgreSQL via Testcontainers: snake_case, multi-mapping, interval, transactions |
| Integration | `MusicStore.IntegrationTests` (30) | Full Aspire app: CRUD through gateway, legacy parity, ProblemDetails, UI smoke |

## 7. Run it

```bash
dotnet run --project src/MusicStore.AppHost
```

The Aspire dashboard opens with all resources (Postgres, pgAdmin, migrations,
catalog, songs, gateway, web). The Blazor UI is the `web` resource; the gateway
is the single entry point for API traffic.

## 8. Migration history

- Tracker: `MIGRATION-TRACKER.md` (36 tasks, 9 phases)
- Key docs: `docs/MIG-08-schema-parity.md`, `docs/MIG-27-legacy-parity.md`,
  `docs/MIG-28-cutover-checklist.md`, `docs/MIG-UI-migration-plan.md`
- Legacy stack: preserved at tag `legacy/final` (deleted from the working tree in MIG-29)

## 9. Lessons learned

- **Register handlers in DI before minimal APIs see them** — unregistered complex-type
  parameters get inferred as body parameters and GET routes fail at startup with a
  misleading error.
- **YARP's forwarder bypasses `IHttpClientFactory`** — service discovery needs a custom
  `IForwarderHttpClientFactory` (YARP 2.3 has no `AddHttpMessageHandler`).
- **YARP cluster config schema is strict**: `Destinations: { name: { Address } }` — a
  singular `Destination` key silently loads zero destinations.
- **`AddDotnetProject` does not infer launchSettings endpoints** — declare
  `.WithHttpEndpoint()` explicitly.
- **`Response.WriteAsJsonAsync` overwrites a manually-set ContentType** — pass the
  content type as a parameter (`application/problem+json`).
- **Domain guards (`ArgumentException`) leak as 500s** — validate in handlers and throw
  `ValidationException` to map to 422 ProblemDetails.
- **Blazor Web App template (this SDK) requires `app.UseAntiforgery()`** — without it
  every page 500s; also remove the unsupported `<BasePath>` element.
- **Interactive Server renders data over SignalR** — initial HTML contains the shell,
  not the data; smoke tests must assert page availability, not rows.
- **End-to-end tests pay for themselves immediately** — the MIG-26 harness surfaced 7
  latent bugs that no lower-level suite caught.
