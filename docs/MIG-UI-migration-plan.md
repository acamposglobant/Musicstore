# UI Migration Plan — Replacing the Silverlight Presentation Layer

> Source: `Legacy/Presentation/` (Silverlight 5 app + Silverlight.Web host, WPF shell, `UI.Process` scaffold).
> Companion docs: [MIGRATION-TRACKER.md](../MIGRATION-TRACKER.md) · [ARCHITECTURE.md](../ARCHITECTURE.md)

---

## 1. What the legacy UI actually is

| Project | Content | Verdict |
|---|---|---|
| `MusicStore.UI.Silverlight` | `App.xaml.cs` boots `MainPage` — an **empty `<Grid>`**. No service references, no WCF client code, no views. | Empty skeleton |
| `MusicStore.UI.Silverlight.Web` | ASP.NET WebForms test page hosting the `.xap` via `<object>` + `Silverlight.js`. | Host scaffolding only |
| `MusicStore.UI.Windows` (WPF) | `MainWindow` — **empty `<Grid>`**, no code-behind logic. | Empty skeleton |
| `MusicStore.UI.Process` | Class library with no classes. | Empty scaffold |

**Conclusion:** there is no implemented UI functionality to port. The migration target must therefore deliver the *intended* capability — a browser-based catalog client consuming the backend API — rather than a line-by-line port. The legacy projects are still deleted in MIG-29 as planned; this plan adds the replacement.

## 2. Target: Blazor Web App (recommended)

**`MusicStore.Web`** — Blazor Web App (.NET 10, Interactive Server rendering), consuming the REST API through the YARP gateway.

Rationale:
- **C# end-to-end** — matches the repo's pure-.NET toolchain (no Node/npm build pipeline, no new CI surface).
- **First-class Aspire integration** — a project like any other: `AddProject<MusicStore.Web>()` + `WithReference(gateway)`.
- **Silverlight's role was "rich browser UI"** — Blazor Server/WebAssembly is the direct modern successor; XAML→Razor component mapping is the most natural porting path.
- Alternative considered: JS SPA (React/Vue/Angular) — viable, but adds a Node toolchain, a second package ecosystem, and CI steps to a currently all-.NET solution.

## 3. Proposed tasks (new Phase 8: Web UI)

| Task | Name | Scope | Effort | Depends on |
|---|---|---|---|---|
| MIG-31 | Scaffold Blazor Web App + Aspire registration | `src/MusicStore.Web` (Blazor Web App, interactive server), typed `CatalogApiClient` (refit or plain `HttpClient` + System.Text.Json) targeting the gateway; registered in AppHost with `WithReference(gateway)`; ProblemDetails → UI error surface. | M | MIG-22 (gateway) |
| MIG-32 | Catalog browse views | Components: Artists list, Genres list, Albums list (with artist/genre names via `?include=artist,genre`), Album detail with songs (`GET /albums/{id}/songs`). Read-only, replaces the empty Silverlight `MainPage` intent. | M | MIG-31, MIG-19 |
| MIG-33 | Catalog management (CRUD) views | Create/edit/delete forms for artists, genres, albums, songs; validation errors (422 ProblemDetails) rendered inline; 404 → friendly not-found page. | M | MIG-32 |
| MIG-34 | UI integration coverage | Extend Aspire integration tests: gateway → UI smoke (page renders, API consumed); OpenAPI contract stays source of truth. | S | MIG-33, MIG-26 |

**Definition of Done additions for this phase:** pages render with zero console errors; all HTTP calls go through the gateway; no hardcoded API URLs (config via Aspire `WithReference`); ProblemDetails surfaced as user-visible messages.

## 4. Sequencing

- Phase 8 starts after **MIG-22** (gateway) — the UI consumes the gateway, not the APIs directly.
- MIG-32 additionally needs **MIG-19** (albums/songs endpoints) for the enriched read paths.
- MIG-29 (legacy deletion) is unaffected: Silverlight/WPF/UI.Process are deleted as planned; this plan replaces their *capability*, not their code.

## 5. Risks / notes

- **Blazor Server** chosen for interactive rendering (simplest ops story under Aspire; no WASM download cost). If offline-capable or CDN-hosted UI is later required, switch to Blazor WebAssembly — the component code is shared.
- The legacy UI never consumed the WCF service, so there is no parity baseline for UI behavior; acceptance is functional (CRUD works against the API), not payload parity.
- If a JS SPA is preferred later, the gateway + OpenAPI contract make the frontend swappable without backend changes.
