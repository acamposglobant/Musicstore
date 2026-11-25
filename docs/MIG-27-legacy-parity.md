# MIG-27 — Legacy Parity Verification Report

> Sign-off gate for Phase 7 (Legacy Decommission). Automated comparison of the legacy
> WCF SOAP payloads vs the migrated REST responses over the same seed data
> (`db/100_seed.sql`). Executed by `tests/MusicStore.IntegrationTests/LegacyParityTests.cs`.

## Legacy contract (WCF SOAP)

`MusicStore.Services.Contracts.IMusicStoreService` (hosted at `MusicStoreService.svc`):

| Operation | Success payload |
|---|---|
| `GetArtist()` | `ServiceReturn { status: "OK", responseData: [ { artistID, name } ] }` |
| `GetGenres()` | `ServiceReturn { status = "OK", responseData: [ { genreID, name, description } ] }` |

Errors were returned in-band as `ServiceReturn { status: "ERROR", exception: ExceptionReturn }`
(`includeExceptionDetailInFaults=true` — exception details leaked to clients).

## Migrated contract (REST via YARP gateway)

| Legacy operation | New endpoint | Parity |
|---|---|---|
| `GetArtist()` | `GET /catalog/artists` | ✅ verified |
| `GetGenres()` | `GET /catalog/genres` | ✅ |

Envelope mapping:

| Legacy | Migrated |
|---|---|
| `status: "OK"` + `responseData: [...]` | `200 OK` + bare JSON array |
| `status: "ERROR"` + `exception` | RFC 9457 ProblemDetails (`404`/`422`/`500`), no detail leakage on 500 |
| `artistID` / `genreID` | `id` (camelCase JSON) |
| `name`, `description` | `name`, `description` (unchanged) |

## Parity assertions (all passing)

- **Artists** — all 4 seeded artists present with legacy ids and names:
  `1 The Midnight Echo`, `2 Ella Fitzgerald`, `3 Kraftwerk`, `4 Ludwig van Beethoven`
- **Genres** — all 4 seeded genres present with legacy ids, names and descriptions:
  `1 Rock`, `2 Jazz`, `3 Electronic`, `4 Classical`
- **Payload shape** — success responses are bare JSON arrays of the entity fields
  (no `ServiceReturn` wrapper, no exception envelope); entity fields serialize camelCase.

## Result

| Check | Result |
|---|---|
| `GetArtist()` → `GET /catalog/artists` | ✅ parity |
| `GetGenres()` → `GET /catalog/genres` | ✅ parity |
| Envelope → HTTP semantics mapping | ✅ pinned by tests |

**Sign-off:** parity verified on the merged MIG-26 stack (20/20 integration tests green).
Phase 7 (legacy decommission) may proceed.
