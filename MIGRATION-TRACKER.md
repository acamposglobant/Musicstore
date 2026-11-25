# MusicStore — Legacy → .NET 10 Migration Tracker

> **Source:** `Legacy/MusicStore.sln` (.NET Framework 4.5.1) · **Target:** .NET 10 / C# 14 microservices with .NET Aspire, Clean Architecture, Dapper, PostgreSQL, xUnit.
> Companion doc: [ARCHITECTURE.md](ARCHITECTURE.md) (current-state analysis).

---

## 1. Migration Overview & Architecture Mapping

### 1.1 Legacy → Target Layer Mapping

| Legacy Layer | Legacy Project / Tech | Target Layer | Target Project / Tech | Notes |
|---|---|---|---|---|
| Hosts | `MusicStore.Hosts.Web` (IIS, `.svc`) | **AppHost / Aspire** | `MusicStore.AppHost` | IIS/WCF hosting replaced by Aspire orchestration |
| — (none) | — | **AppHost / Aspire** | `MusicStore.ServiceDefaults` | Health checks, OTel, resilience, service discovery |
| Services (SOAP contract) | `IMusicStoreService`, `ServiceReturn` | **API (Minimal APIs)** | `MusicStore.Api` (Minimal APIs, `TypedResults`) | `ServiceReturn`/`ExceptionReturn` eliminated → RFC 9457 `ProblemDetails` |
| Services (impl) | `MusicStoreService`, `BaseService` | **API (Minimal APIs)** | Endpoint groups per resource | Singleton + no DI replaced by scoped DI |
| Business | `MusicStoreComponent`, `IMusicStoreComponent` | **Application** | `MusicStore.Application` (handlers, DTOs, interfaces) | Vertical-slice handlers |
| Data (interfaces) | `MusicStore.Data` (`IRepositoryArtist`…) | **Application / Domain** | Repository interfaces (`IArtistRepository`…) | Owned by Application, implemented by Infrastructure |
| Data (EF6 EDMX) | `MusicStore.Data.Repository`, `BaseRepository`, `UnitOfWork` | **Infrastructure (Dapper)** | `MusicStore.Infrastructure` (`NpgsqlDataSource`, Dapper) | EF6/EDMX/T4 deleted; `GetGenres` dispose bug designed away |
| Database | `MusicStore.Database` (T-SQL sqlproj), SQL Server LocalDB | **PostgreSQL / DbUp** | `MusicStore.DbMigrations` (DbUp) + PostgreSQL container | `nchar`→`varchar`, `time`→`interval`, no hardcoded paths |
| Entities | `MusicStore.Entities` (T4 POCOs) | **Domain** | `MusicStore.Domain` (pure C#, invariants) | No EF/Dapper attributes |
| Framework | `MusicStore.Framework` (`WrapperLogger`, NLog) | **ServiceDefaults** | OpenTelemetry + `ILogger` | NLog wrapper retired |
| Presentation | Silverlight, WPF, `UI.Process` | **Gateway (YARP)** + SPA/API clients | `MusicStore.Gateway` (YARP) | Obsolete UIs decommissioned; gateway fronts APIs |
| Tests | `MusicStore.Data.Test`, `MusicStore.WCF.Test` (MSTest, real DB) | **Tests (xUnit)** | Unit (NSubstitute) + Integration (`Aspire.Hosting.Testing`) | Real PostgreSQL container, no shared machine state |

### 1.2 Target Solution Layout

```
MusicStore.sln
├── MusicStore.AppHost/              # Aspire orchestrator (PostgreSQL, pgAdmin, services, gateway)
├── MusicStore.ServiceDefaults/      # Health checks, OTel, resilience, service discovery
├── src/
│   ├── MusicStore.Domain/           # Pure C# entities: Artist, Genre, Album, Song
│   ├── MusicStore.Application/      # Handlers, DTOs, repository interfaces, validation
│   ├── MusicStore.Infrastructure/   # Dapper repositories, NpgsqlDataSource, DbUp runner
│   ├── MusicStore.Api/              # Catalog Minimal API (artists, genres, albums)
│   ├── MusicStore.Songs.Api/        # Songs microservice (Phase 5 split)
│   └── MusicStore.Gateway/          # YARP reverse proxy
├── db/
│   └── Scripts/                     # PostgreSQL DDL + seed (DbUp embedded resources)
└── tests/
    ├── MusicStore.Application.UnitTests/    # xUnit + NSubstitute
    ├── MusicStore.Infrastructure.Tests/     # Dapper + PostgreSQL
    └── MusicStore.IntegrationTests/         # Aspire.Hosting.Testing + real container
```

### 1.3 Definition of Done (applies to EVERY task)

A task is **Done** only when ALL of the following hold:

- [ ] `dotnet build` succeeds with **zero warnings** (`TreatWarningsAsErrors` on).
- [ ] All **xUnit** tests pass: unit tests green, and integration tests green against a real PostgreSQL container via `Aspire.Hosting.Testing`.
- [ ] **Aspire dashboard health checks are green** for every affected service (`/health` + `/alive`).
- [ ] No legacy artifacts introduced: no `ServiceReturn`/`ExceptionReturn`, no `nchar`/`time` columns, no hardcoded connection strings/paths, no Singleton service state.
- [ ] Code follows Clean Architecture dependency rule: `Domain ← Application ← Infrastructure/API`; Domain has zero package references.
- [ ] Public API surface documented with OpenAPI and returns `TypedResults` (no anonymous `Ok()`).
- [ ] `Legacy/` code referenced by the task is either replaced or explicitly listed for decommission (Phase 7).

---

## 2. Progress Summary

> Update the tally below as statuses change (this doc is plain Markdown; keep the counts in sync with the table in section 3).

| Metric | Value |
|---|---|
| **Total Tasks** | 30 |
| **Not Started** | 12 |
| **In Progress** | 0 |
| **In Review** | 0 |
| **Blocked** | 0 |
| **Completed** | 18 |
| **Completion %** | `▓▓▓▓▓▓░░░░ 60%` |

**Tasks by Phase**

| Phase | Tasks | Done |
|---|---|---|
| Phase 1: Aspire & Solution Foundation | 4 | 4 |
| Phase 2: PostgreSQL & Schema Migration | 4 | 4 |
| Phase 3: Domain & Dapper Infrastructure | 7 | 7 |
| Phase 4: Minimal APIs & WCF Replacement | 5 | 3 |
| Phase 5: Microservices Split & YARP Gateway | 3 | 0 |
| Phase 6: xUnit & Aspire Integration Testing | 4 | 0 |
| Phase 7: Legacy Decommission | 3 | 0 |

---

## 3. Migration Tasks

**Select-list values** (keep exactly these strings in the cells):

- **Phase:** `Phase 1: Aspire & Solution Foundation` · `Phase 2: PostgreSQL & Schema Migration` · `Phase 3: Domain & Dapper Infrastructure` · `Phase 4: Minimal APIs & WCF Replacement` · `Phase 5: Microservices Split & YARP Gateway` · `Phase 6: xUnit & Aspire Integration Testing` · `Phase 7: Legacy Decommission`
- **Target Layer:** `AppHost / Aspire` · `PostgreSQL / DbUp` · `Domain` · `Application` · `Infrastructure (Dapper)` · `API (Minimal APIs)` · `Gateway (YARP)` · `Tests (xUnit)`
- **Effort:** `XS (1-2h)` · `S (2-4h)` · `M (4-8h)` · `L (1-2d)`
- **Status:** `Not Started` · `In Progress` · `In Review` · `Blocked` · `Done`

| Task ID | Phase | Task Name | Description & Acceptance Criteria | Target Layer | Legacy Reference | Effort | Status | Dependencies |
|---|---|---|---|---|---|---|---|---|
| MIG-01 | Phase 1: Aspire & Solution Foundation | Create .NET 10 solution with Aspire AppHost + ServiceDefaults | `dotnet new aspire` scaffold: `MusicStore.AppHost`, `MusicStore.ServiceDefaults`. Add `Aspire.Hosting.AppHost`, `Microsoft.Extensions.Http.Resilience`, OpenTelemetry packages. AppHost runs and shows dashboard. | AppHost / Aspire | `MusicStore.sln` (new root solution) | S (2-4h) | Done | — |
| MIG-02 | Phase 1: Aspire & Solution Foundation | Scaffold Clean Architecture projects | Create `src/MusicStore.Domain` (net10.0, zero deps), `MusicStore.Application` (refs Domain), `MusicStore.Infrastructure` (refs Application), `MusicStore.Api` (refs Application+Infrastructure). Enforce dependency rule with architecture tests stub. Add all to solution. | Domain | `MusicStore.Entities`, `MusicStore.Business` | S (2-4h) | Done | MIG-01 |
| MIG-03 | Phase 1: Aspire & Solution Foundation | Register PostgreSQL container + pgAdmin in `AppHost.cs` | In `AppHost.cs`: `AddPostgres("musicstore-db")` with data volume, `WithPgAdmin()`, `AddDatabase("musicstore")`. Dashboard shows DB resource healthy. | AppHost / Aspire | SQL Server LocalDB `MusicStore.mdf` | S (2-4h) | Done | MIG-01 |
| MIG-04 | Phase 1: Aspire & Solution Foundation | Centralized configuration & connection injection | Remove hardcoded paths pattern: connection strings flow from AppHost (`WithReference(database)`) into services via `builder.AddNpgsqlDataSource(...)`-ready config. Add `Options` classes; no `App.config`/`Web.config` equivalents. | AppHost / Aspire | `Web.config` connectionStrings (hardcoded LocalDB path) | XS (1-2h) | Done | MIG-03 |
| MIG-05 | Phase 2: PostgreSQL & Schema Migration | Convert T-SQL schema to PostgreSQL DDL | Author `db/001_create_tables.sql`: `artist`, `genre`, `album`, `song` tables. Fix legacy debt: `nchar(100)`→`varchar(100)` (no padding), `time(7)`→`interval`, `IDENTITY`→`GENERATED ALWAYS AS IDENTITY`, FKs `FK_Album_Artist`, `FK_Album_Genre`, `FK_Song_Album` with `ON DELETE` rules. | PostgreSQL / DbUp | `MusicStore.Database.sqlproj`, `MusicStoreModel.edmx` (SSDL) | M (4-8h) | Done | MIG-03 |
| MIG-06 | Phase 2: PostgreSQL & Schema Migration | DbUp migration runner wired into AppHost | Create `MusicStore.DbMigrations` console runner (DbUp 6.x, embedded scripts from `db/`, journal table `schemaversions`). Registered in AppHost with `WaitFor(database)`; Api waits for migrations. Idempotent re-run verified. | PostgreSQL / DbUp | `MusicStore.Database.sqlproj` deploy | M (4-8h) | Done | MIG-05 |
| MIG-07 | Phase 2: PostgreSQL & Schema Migration | Seed data scripts | `db/100_seed.sql`: sample artists, genres, albums, songs matching legacy test data (see `Tests/MusicStore.Test`). Runs via DbUp in a `Seed` script tagged to run once. | PostgreSQL / DbUp | `UnitTestArtist.cs` fixtures | XS (1-2h) | Done | MIG-06 |
| MIG-08 | Phase 2: PostgreSQL & Schema Migration | Schema parity verification | Script/queries verifying: PK/FK constraints, unique names, `varchar` (no `bpchar` padding), `interval` durations, indexes on FK columns. Document parity checklist result in PR. | PostgreSQL / DbUp | `MusicStoreModel.edmx` SSDL | S (2-4h) | Done | MIG-06 |
| MIG-09 | Phase 3: Domain & Dapper Infrastructure | Pure C# domain entities | `Domain/Artist.cs`, `Genre.cs`, `Album.cs`, `Song.cs`: int IDs, required strings, `TimeSpan Duration` (maps to `interval`), navigation collections. Invariants enforced in constructors (non-empty name, positive duration). No ORM attributes. | Domain | `MusicStore.Entities/*.cs` (T4 POCOs) | S (2-4h) | Done | MIG-02 |
| MIG-10 | Phase 3: Domain & Dapper Infrastructure | Repository interfaces | `Application/Interfaces/IArtistRepository.cs`, `IGenreRepository.cs`, `IAlbumRepository.cs`, `ISongRepository.cs`: async CRUD (`GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`) returning Domain types. | Application | `MusicStore.Data/IRepository*.cs` | XS (1-2h) | Done | MIG-02 |
| MIG-11 | Phase 3: Domain & Dapper Infrastructure | NpgsqlDataSource + Dapper conventions | `Infrastructure/DependencyInjection.cs`: register singleton `NpgsqlDataSource` from Aspire connection string; `DefaultTypeMap.MatchNamesWithUnderscores = true`; snake_case mapping verified with a smoke query. | Infrastructure (Dapper) | `MusicStoreModel.Context.cs` (DbContext) | S (2-4h) | Done | MIG-06, MIG-02 |
| MIG-12 | Phase 3: Domain & Dapper Infrastructure | Dapper Artist & Genre repositories | `Infrastructure/Repositories/ArtistRepository.cs`, `GenreRepository.cs`: parameterized SQL, `NpgsqlDataSource.CreateConnection()`, no shared-connection dispose bugs (connection per operation — fixes `GetGenres` bug class). | Infrastructure (Dapper) | `RepositoryArtist.cs`, `RepositoryGenre.cs`, `BaseRepository.cs` | S (2-4h) | Done | MIG-11 |
| MIG-13 | Phase 3: Domain & Dapper Infrastructure | Dapper AlbumRepository with multi-mapping | `AlbumRepository`: `GetAllWithArtistAndGenreAsync` using Dapper multi-mapping (`QueryAsync<Album, Artist, Genre, Album>`) joining `album ⋈ artist ⋈ genre`; identity split on `ArtistID`/`GenreID`. | Infrastructure (Dapper) | `RepositoryAlbum.cs`, EDMX navigation properties | M (4-8h) | Done | MIG-12 |
| MIG-14 | Phase 3: Domain & Dapper Infrastructure | Dapper SongRepository (interval mapping) | `SongRepository`: `TimeSpan` ↔ PostgreSQL `interval` type handler (`NpgsqlTypeHandler`/Dapper type map). CRUD + `GetByAlbumIdAsync`. | Infrastructure (Dapper) | `RepositorySong.cs` | S (2-4h) | Done | MIG-12 |
| MIG-15 | Phase 3: Domain & Dapper Infrastructure | Transaction/unit-of-work replacement | `Infrastructure/IUnitOfWork.cs` + implementation wrapping `NpgsqlTransaction` over a scoped connection; replaces legacy `UnitOfWork` (fix `Roolback` typo semantics; commit = transaction commit, no implicit `SaveChanges`). | Infrastructure (Dapper) | `Infrastructure/UnitOfWork.cs` | S (2-4h) | Done | MIG-11 |
| MIG-16 | Phase 4: Minimal APIs & WCF Replacement | Application handlers + DTOs (kill ServiceReturn) | `Application/Artists/GetArtists/` etc. (handler + `ArtistDto` + mapping). No `ServiceReturn`/`object responseData` — handlers return typed results; failures throw domain exceptions mapped to HTTP later. | Application | `MusicStoreComponent.cs`, `ServiceReturn.cs`, `ExceptionReturn.cs` | M (4-8h) | Done | MIG-10 |
| MIG-17 | Phase 4: Minimal APIs & WCF Replacement | Global error handling → RFC 9457 ProblemDetails | `AddProblemDetails()`, exception handler mapping `KeyNotFoundException`→404, `ValidationException`→422, unexpected→500 (no stack traces; replaces `includeExceptionDetailInFaults=true` + `FormatException`). | API (Minimal APIs) | `BaseService.LogServiceException/FormatException` | S (2-4h) | Done | MIG-16 |
| MIG-18 | Phase 4: Minimal APIs & WCF Replacement | Artists & Genres Minimal API endpoints | `Api/Endpoints/ArtistEndpoints.cs`, `GenreEndpoints.cs`: `GET /artists`, `GET /artists/{id}`, `POST`, `PUT`, `DELETE` with `TypedResults.Ok/Created/NoContent/NotFound`, request validation, OpenAPI metadata. Replaces `GetArtist()`/`GetGenres()` SOAP ops. | API (Minimal APIs) | `IMusicStoreService`, `MusicStoreService` | M (4-8h) | Done | MIG-12, MIG-16 |
| MIG-19 | Phase 4: Minimal APIs & WCF Replacement | Albums & Songs Minimal API endpoints | CRUD endpoints incl. `GET /albums?include=artist,genre` (multi-mapping read path) and `GET /albums/{id}/songs`. Same `TypedResults` + ProblemDetails conventions. | API (Minimal APIs) | `MusicStoreService`, `GetArtistResponse` | M (4-8h) | Not Started | MIG-13, MIG-14 |
| MIG-20 | Phase 4: Minimal APIs & WCF Replacement | Observability & API polish | OpenAPI UI, request logging, OTel traces span DB calls (via ServiceDefaults), rate of `LogCritical` parity with NLog wrapper. Structured logging replaces `WrapperLogger`. | API (Minimal APIs) | `MusicStore.Framework/WrapperLogger.cs`, `NLog.config` | S (2-4h) | Not Started | MIG-18 |
| MIG-21 | Phase 5: Microservices Split & YARP Gateway | Extract Songs microservice | Move song endpoints + `SongRepository` into `MusicStore.Songs.Api` with its own DB schema/role; catalog API no longer serves songs. Shared Domain via project reference or NuGet split — document choice. | API (Minimal APIs) | `MusicStoreService` (monolith service) | M (4-8h) | Not Started | MIG-19 |
| MIG-22 | Phase 5: Microservices Split & YARP Gateway | YARP API Gateway + service discovery | `MusicStore.Gateway` with YARP routes `/catalog/{**catch-all}` → catalog API, `/songs/{**catch-all}` → songs API; endpoints resolved via Aspire service discovery (`AddServiceDiscovery`). Registered in AppHost. | Gateway (YARP) | `MusicStore.Hosts.Web` (single SOAP endpoint) | M (4-8h) | Not Started | MIG-21 |
| MIG-23 | Phase 5: Microservices Split & YARP Gateway | Resilience & health for downstream calls | Standard resilience handlers (retry/circuit-breaker/timeout) on gateway + inter-service `HttpClient`; health checks aggregate DB + downstream; dashboard all green. | AppHost / Aspire | `myWCFMessages.xml` tracing (replaced by OTel) | S (2-4h) | Not Started | MIG-22 |
| MIG-24 | Phase 6: xUnit & Aspire Integration Testing | xUnit unit tests for Application handlers | `MusicStore.Application.UnitTests`: NSubstitute mocks of repository interfaces; cover happy path + not-found + validation for every handler. Zero DB access. | Tests (xUnit) | `MusicStore.Data.Test` (MSTest) | M (4-8h) | Not Started | MIG-16 |
| MIG-25 | Phase 6: xUnit & Aspire Integration Testing | Dapper repository tests against PostgreSQL | `MusicStore.Infrastructure.Tests`: real PostgreSQL (Testcontainers or Aspire testing `DistributedApplicationTestingBuilder`); verify snake_case mapping, multi-mapping, interval round-trip, transaction rollback. | Tests (xUnit) | `UnitTestArtist/Album/Genre/Song.cs` | M (4-8h) | Not Started | MIG-13, MIG-14 |
| MIG-26 | Phase 6: xUnit & Aspire Integration Testing | End-to-end Aspire integration tests | `MusicStore.IntegrationTests` using `Aspire.Hosting.Testing`: spin AppHost, wait for health checks, exercise HTTP endpoints (CRUD for all 4 entities) through the gateway, assert RFC 9457 error responses. | Tests (xUnit) | `MusicStore.WCF.Test/UnitTestMusicStoreService.cs` | L (1-2d) | Not Started | MIG-22, MIG-24 |
| MIG-27 | Phase 6: xUnit & Aspire Integration Testing | Legacy parity verification | Automated comparison: legacy `GetArtist()`/`GetGenres()` SOAP payloads vs new REST responses (same seed data). Parity report checked into `docs/`. Sign-off gate for Phase 7. | Tests (xUnit) | `IMusicStoreService`, `ServiceReturn` payloads | S (2-4h) | Not Started | MIG-26 |
| MIG-28 | Phase 7: Legacy Decommission | Cutover checklist & freeze legacy | Verify parity report, export final SQL Server data snapshot, tag `legacy/final` in git, freeze `Legacy/` (no further changes). | PostgreSQL / DbUp | `Legacy/` (whole tree) | S (2-4h) | Not Started | MIG-27 |
| MIG-29 | Phase 7: Legacy Decommission | Delete legacy projects | Remove `Legacy/` from repo: WCF host/services/contracts, Silverlight + Silverlight.Web, WPF shell, `UI.Process`, `Data.Agents`, EDMX/T4 projects, MSTest projects, `backup.MusicStoreRepository.cs`. Solution builds clean with only new projects. | API (Minimal APIs) | `MusicStore.sln` legacy entries | S (2-4h) | Not Started | MIG-28 |
| MIG-30 | Phase 7: Legacy Decommission | Final docs & architecture update | Rewrite `ARCHITECTURE.md` for the target state (Aspire + Clean Architecture + Dapper/PostgreSQL), update README with run instructions (`dotnet run --project AppHost`), record lessons learned. | Tests (xUnit) | `ARCHITECTURE.md`, `README.md` | XS (1-2h) | Not Started | MIG-29 |

---

## 4. Working Conventions

- **Branching:** one branch per task (`mig/MIG-xx-short-name`), PR per task, tracker `Status` → `In Review` when PR opens, `Done` on merge with DoD met.
- **Vertical slices:** tasks MIG-12→MIG-18 and MIG-13/14→MIG-19 can each be delivered as thin end-to-end slices (repo → handler → endpoint → test) rather than big-bang layers.
- **No big-bang cutover:** Phase 7 only starts after MIG-27 parity sign-off.
- **Bug-fix guarantees carried into target:** connection-per-operation (no shared `DbContext` dispose bug), typed contracts (no `object` payloads), `varchar` (no `nchar` padding), config via Aspire (no machine-specific paths).
