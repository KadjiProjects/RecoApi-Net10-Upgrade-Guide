> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 1 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Next: [Phase 0 — Discovery](01-discovery.md)

# RecoAPI Upgrade Playbook — .NET 6 → .NET 10 + AutoMapper Removal

> **Audience: an AI coding agent.** This document is fully self-contained: every file you
> must create is given **verbatim**, every edit is given as an exact before/after, and every
> phase ends with a **GATE** you must pass before continuing. You do not need any other
> document, and you must not guess anything that this document specifies.

---

## 0. Ground rules — read completely before doing anything

### 0.1 What you are upgrading

A .NET 6 solution with this architecture (project names may carry the `RECO.API.` prefix):

- **Domain** — data-model records (`TblCode`, `TblCodeType`, …) and shared interfaces.
- **Application** — module abstractions (filters, subscribers), persistence interfaces.
- **Filter / Subscriber projects** — one or more thin projects implementing module bases.
- **Utils** — logging helpers including a custom Serilog *email sink* wrapper.
- **Persistence** — EF Core repositories, DB models mirroring the domain records, and an
  **AutoMapper `MapperFactory`** class containing all mapping configuration.
- **PersistentQueue / Processing** — queue and a generic pipeline
  `ActionProcessor<TDataModel, TDbModel>` that maps between its two type parameters.
- **ModulesLoading** — runtime plugin loader (`Assembly.LoadFile`).
- **Service** — ASP.NET Core host (`Program`/`Startup`), Swashbuckle, Serilog config in
  `appsettings.json`.
- **Test project(s)** — NUnit 3.

### 0.2 Your copy may differ from the baseline — how to handle variance

The concrete numbers in this document (11 projects, 8 entities, 24 maps, 4 connection
strings) describe the **baseline** codebase. Your copy is guaranteed to match
**architecturally**, but it may contain **more filter or subscriber projects, more
entities in the MapperFactory, more connection strings, or additional files**. Therefore:

- **Never trust a count — always enumerate.** Every phase starts with a discovery command
  whose output defines *your* work list. Where this document says "all projects" or
  "every entity", it means the list *you* enumerated, not the baseline numbers.
- **Where verbatim code is given for baseline entities, verify before use.** Each verbatim
  file has a *precondition* (the exact property list it assumes). If your model matches,
  use the file unchanged. If your model has extra/renamed properties, or your
  MapperFactory has extra entities, apply the **derivation algorithm** in §4.2 instead —
  it produces the same result mechanically.
- **Extra filter/subscriber projects** follow the Filter/Subscriber pattern: they contain
  no AutoMapper usage and no NuGet packages of their own. For each such project the only
  change is the target framework (§2.1). Do not skip them.

### 0.3 Hard rules (do NOT deviate)

1. **DO NOT** modernize anything not listed here: keep `Startup`/`IHostBuilder` hosting,
   keep NUnit 3.x assert style, keep the project layout, keep namespaces, keep file
   formatting of untouched code.
2. **DO NOT** upgrade NUnit to 4.x (it removes classic asserts and breaks existing tests).
3. **DO NOT** switch Swashbuckle for a different OpenAPI package.
4. **DO NOT** delete or alter the `ResSchemaGenerator` `<Reference>` in the Service
   project, even though no code uses it (runtime plugins may resolve the DLL).
5. **DO NOT** install Mapperly, AutoMapper 12+, or any other mapping package. The
   replacement is the hand-written library in §3, exactly as given.
6. **DO NOT** edit `.cs` files beyond the edits specified here, except where a GATE
   fails and the Troubleshooting table (§7) tells you to.
7. **DO** run every GATE. If a GATE fails and §7 does not cover the failure, stop and
   report — do not improvise.

### 0.4 Prerequisites

- .NET SDK 10.0.100+ installed. Verify: `dotnet --list-sdks` shows a `10.*` entry.
- NuGet.org reachable.
- No SQL Server needed for any GATE (tests that need one are classified in §6.4).

---

