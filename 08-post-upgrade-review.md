> **RecoAPI .NET 6 → .NET 10 upgrade guide — supplementary.** This file is NOT part of the
> mandatory execution sequence (parts 1–10). Apply it AFTER GATE 5 passes, as a separate,
> reviewable change set. Everything here comes from an independent post-upgrade code review
> of the completed baseline upgrade.

## 8. Post-upgrade review — known issues & hardening recommendations

The upgrade in parts 1–10 is deliberately conservative: it changes only what .NET 10
forces. The completed baseline builds with **0 warnings / 0 errors** and passes all
mapping tests — but the review found the following pre-existing or upgrade-adjacent
issues that a fork owner should decide about consciously. Each item states the issue,
the risk, and the recommended action. None of them block the upgrade.

### 8.1 Dead binary reference: `ResSchemaGenerator.dll` (netcoreapp2.2)

**What:** the Service project carries a raw `<Reference>` to a prebuilt
`ResSchemaGenerator.dll` targeting `netcoreapp2.2` (a 2019-era runtime). A solution-wide
search shows **no C# code references it** — it is copied to the output for nothing,
unless a *runtime-loaded plugin* (filter/subscriber module) resolves it by name.

**Risk:** if a plugin ever does call into it, a .NET Core 2.2 assembly running on the
.NET 10 runtime may fail at runtime on removed/changed APIs — invisible at compile time.
If nothing calls it, it is dead weight that confuses every future reader and scanner.

**Action:** ask the operators whether any deployed module uses it. If no →
remove the `<Reference>` block and the `References/` folder. If yes → rebuild that
utility from source against `net10.0` and replace the DLL. Do NOT do this silently as
part of the upgrade (hard rule §0.3-4 exists precisely so the upgrade stays reversible).

### 8.2 Absolute developer paths in `appsettings.json`

**What:** `FiltersLocation` and `SubscribersLocation` point at absolute paths from the
original developer's machine (pattern: `C:\...\src\Service\RECO.API.Service\Modules\...`).

**Risk:** on any other machine, module discovery finds **zero** filters/subscribers.
Depending on the fork's modules this is either a silent behavior change (no filters run)
or a broken deployment. This is the single most likely "it upgraded fine but doesn't
work" cause for a fork.

**Action:** make the paths relative to the content root and resolve them at startup,
e.g. store `"FiltersLocation": "Modules/Filters"` and combine with
`env.ContentRootPath` in `Startup.SetupModules` (or accept both absolute and relative).
At minimum, override per machine via environment variables
(`FiltersLocation=...` / `SubscribersLocation=...`) or `appsettings.Development.json`.

### 8.3 Integration tests hardcode a developer SQL Server instance

**What:** the PersistentQueue NUnit tests contain a hardcoded connection string to a
named local SQL Server instance and create/drop a real database. On any machine without
that instance they fail with `SqlException` error 26 (server not found) — which is
exactly what a fresh clone sees.

**Risk:** CI cannot run them; worse, an LLM performing the upgrade may misread the
failures as an upgrade regression. (§6.4 classifies them as environment-dependent for
exactly this reason.)

**Action:** read the connection string from an environment variable with the current
value as fallback, and mark the tests `[Explicit]` or skip when the variable is unset.
Example:

```csharp
private readonly string _connectionString =
    Environment.GetEnvironmentVariable("RECOAPI_TEST_DB")
    ?? "Server=localhost;Database=TestDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
```

### 8.4 `Assembly.LoadFile` plugin loading has no dependency resolution

**What:** `ModuleProviderLoader` loads filter/subscriber DLLs with
`Assembly.LoadFile(path)`. On modern .NET this loads the assembly in isolation: the
plugin's **own NuGet dependencies are not resolved** from its folder (no `.deps.json`
processing), and types are not shared with identically-named assemblies loaded elsewhere.

**Risk:** works today because the baseline modules only use types from the host. The
first fork that ships a module with its own package dependency gets
`FileNotFoundException` at runtime, far from the cause.

**Action (when needed):** switch to a custom `AssemblyLoadContext` with
`AssemblyDependencyResolver` — the documented .NET plugin pattern — which resolves each
plugin's dependencies from its own directory while sharing the host's contract
assemblies. Not required while modules stay dependency-free.

### 8.5 `TrustServerCertificate=True` is a development setting

**What:** §2.4 appends `TrustServerCertificate=True;` to every connection string because
`Microsoft.Data.SqlClient` 4+ defaults to `Encrypt=true`, which fails against servers
with self-signed certificates.

**Risk:** `TrustServerCertificate=True` encrypts the connection but **skips certificate
validation** — it accepts any certificate, including an active man-in-the-middle's.
Acceptable on a developer machine; not what you want between a production API and a
production database.

**Action for production forks:** install a certificate the client trusts on the SQL
Server (or use the CA-issued one), then remove `TrustServerCertificate=True` and keep
the default `Encrypt=True` — or step up to `Encrypt=Strict` (TDS 8.0) where supported.

### 8.6 The email sink accepts any SMTP certificate

**What:** `EmailSinkExtensions.ValidateServerCertificate` returns `true`
unconditionally — preserved verbatim from the .NET 6 original to keep behavior identical.

**Risk:** same class of issue as 8.5, for the SMTP TLS connection: log emails (which can
contain operational details) are encrypted but the server is never authenticated.

**Action:** if the mail server has a valid certificate, delete the callback assignment
and let MailKit validate normally; keep the override only for genuinely self-signed
internal relays.

### 8.7 `appsettings.Development.json` is loaded as *required*

**What:** `Program.Main` builds its bootstrap configuration with
`.AddJsonFile("appsettings.Development.json", optional: false)` — the file is mandatory
in every environment, including production publishes.

**Risk:** a deployment that (reasonably) omits the Development file crashes at startup
with `FileNotFoundException` before any logging is configured.

**Action:** change to `optional: true` (and let environment-specific configuration come
from `ASPNETCORE_ENVIRONMENT` + the host's standard configuration stack, which
`CreateDefaultBuilder` already provides).

### 8.8 Minor observations (no action strictly needed)

- The solution file gained x64/x86 solution platforms that all map to AnyCPU — harmless
  IDE-generated noise; safe to keep or strip.
- `Startup` keeps both a private `_configuration` field and a public `Configuration`
  property (the property was unassigned before the upgrade — CS8618). Consolidating to
  one is a cosmetic cleanup.
- Swagger UI is registered only for the Development environment — intentional in the
  baseline; forks exposing the API to consumers may want it behind auth instead.

---

**Sequencing recommendation:** land the upgrade (parts 1–10) as one commit, then apply
each 8.x item you adopt as its own commit — small, reviewable, individually revertable.
