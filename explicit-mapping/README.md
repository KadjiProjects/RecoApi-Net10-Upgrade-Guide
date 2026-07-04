# ExplicitMapping — a dependency-free AutoMapper replacement, with an LLM-executable migration guide

A small, hand-written, compile-time-safe mapping library plus a complete, project-agnostic
guide for removing AutoMapper from any .NET codebase and replacing it with explicit code.
Everything in this folder is self-contained and generic — it applies to any .NET project,
on any target framework.

## What's here

```
explicit-mapping/
├─ src/ExplicitMapping/          the library — 7 files, zero package dependencies
│   ├─ IMappedFrom.cs            construct-from contract (static abstract member)
│   ├─ IMapsTo.cs                project-to contract
│   ├─ IScalarCopyable.cs        clone / in-place scalar copy (leaves navigations untouched)
│   ├─ IDualMapped.cs            the three above, combined, for generic pipelines
│   ├─ MappingExtensions.cs      MapFromAll / MapToAll list helpers
│   └─ Verification/
│       ├─ MappingVerifier.cs             the AssertConfigurationIsValid() replacement
│       └─ MappingVerificationException.cs
├─ tests/ExplicitMapping.Tests/  8 NUnit tests exercising every contract + the verifier
├─ guide/                        the AutoMapper → ExplicitMapping migration guide
│   ├─ README.md                       start here (multi-file, GATE-per-file)
│   ├─ 00…08-*.md                      the nine phase files
│   ├─ AUTOMAPPER-REMOVAL-GUIDE.md      single-file edition (same content, one document)
│   └─ automapper-removal-guide.html    styled single-page HTML edition
├─ ExplicitMapping.sln
└─ LICENSE
```

## The library in 60 seconds

AutoMapper resolves mappings at runtime by reflection and convention; a forgotten property
fails at runtime (or silently maps to a default). ExplicitMapping is the opposite: every
mapping is ordinary hand-written C# you can read and step through, a missing mapping is a
**compile error** (via the interface contracts) or a **failing test** (via `MappingVerifier`),
and the library itself pulls in **no NuGet dependencies at all**.

Two usage tiers:

- **Interface tier** (C# 11 / .NET 7+): implement `IMappedFrom` / `IMapsTo` /
  `IScalarCopyable` / `IDualMapped` for generic-constraint call sites
  (`TDest.MapFrom(source)`, `source.MapTo()`) with zero runtime indirection.
- **Fallback tier** (any C# version / .NET Framework / .NET Standard): plain static or
  extension methods. `MappingVerifier` works identically either way — it only needs a
  `Func<TSource, TDest>`.

## Build and test it

```bash
dotnet build ExplicitMapping.sln     # 0 warnings, 0 errors
dotnet test  ExplicitMapping.sln     # 8/8 passed
```

## Migrate a project off AutoMapper

Open **[guide/README.md](guide/README.md)** and execute the phases in order. The guide is
written for an AI coding agent (and works fine for a human): it starts with a discovery
step that inventories *your* codebase, gives a 16-pattern catalog covering the entire
AutoMapper feature surface with exact replacement code for each, walks call-site rewriting
and the tricky special cases (DI-dependent resolvers, inheritance, reference cycles,
`ProjectTo` LINQ projections), and ends with a `MappingVerifier`-based verification pass
and an acceptance checklist. Prefer one file? Use
**[guide/AUTOMAPPER-REMOVAL-GUIDE.md](guide/AUTOMAPPER-REMOVAL-GUIDE.md)**; prefer a browser?
Open **[guide/automapper-removal-guide.html](guide/automapper-removal-guide.html)**.

## License

MIT — see [LICENSE](LICENSE). Use it, copy the `src/ExplicitMapping/` folder straight into
your own solution, adapt it freely.
