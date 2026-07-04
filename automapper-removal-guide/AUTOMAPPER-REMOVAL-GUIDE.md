# Replacing AutoMapper with RECO.Mapping — a generic, LLM-executable guide

*Single-file edition — the same content as this folder's [README](README.md) and its
nine numbered files (`00-ground-rules.md` … `08-final-checklist.md`), concatenated into
one document for contexts where fetching or pasting a single file is easier than
navigating a multi-file repo. Both editions are kept in sync; use whichever fits your
tool. See the multi-file [README](README.md) for the per-file version with GitHub-native
navigation.*

> **This guide is independent of the RecoAPI upgrade playbook** in this repository's
> root. That playbook upgrades one specific codebase (RecoAPI) from .NET 6 to .NET 10
> end-to-end, of which AutoMapper removal is one phase among many. **This guide is
> scoped to exactly one thing — removing AutoMapper and replacing it with the
> `RECO.Mapping` library — and is written to apply to *any* .NET project**, on any
> target framework, whether or not that project is also being upgraded to .NET 10.

## What this guide assumes about you (the executor)

You are an AI coding agent (or a human following along) with:

- Read/write access to a .NET codebase that references the `AutoMapper` NuGet package
  somewhere — `Profile` classes with `CreateMap<>()` calls, `IMapper` injected via DI,
  `services.AddAutoMapper(...)` registration, or `.ProjectTo<T>()` LINQ usage.
- No prior knowledge of this specific codebase's mapping layout. Every phase starts with
  a **discovery step** that produces *your* work list — this guide never assumes a fixed
  number of mappings, entities, or projects, because "any project" means the shape is
  unknown until you look.
- The ability to run `dotnet build` / `dotnet test` and read their output.

## The library you are migrating to

`RECO.Mapping` — a small, dependency-free library of mapping contracts and a test-time
coverage verifier. Its complete, buildable source lives in this repository at
[`../src/RECO.Mapping/`](../src/RECO.Mapping/) (7 files, zero package dependencies), with
a working demo/test project at [`../tests/RECO.Mapping.Tests/`](../tests/RECO.Mapping.Tests/)
you can build and run right now with `dotnet build ../RECO.Mapping.sln && dotnet test
../RECO.Mapping.sln` to see it work before touching your own project.

Two usage tiers, chosen per mapping pair depending on what you own and what C# version
you target (full decision rules in [§2](#add-the-library)):

1. **Interface-based** (`IMappedFrom`/`IMapsTo`/`IScalarCopyable`/`IDualMapped`) — requires
   C# 11 / .NET 7+ (static abstract interface members) and that you can add an interface
   to the mapped type's declaration. Gives you generic-constraint call sites
   (`TDest.MapFrom(source)`, `source.MapTo()`) with zero runtime indirection.
2. **Plain static methods** — works on **any** C# version, any target framework, and any
   type you don't own (sealed, generated, third-party). No interfaces, just ordinary
   static/extension methods. `MappingVerifier` (the AutoMapper `AssertConfigurationIsValid`
   replacement) works identically either way — it only needs a `Func<TSource,TDest>`.

## How to execute (instructions for the LLM)

1. Read [§0 Ground rules](#ground-rules) completely before touching anything.
2. Execute the sections in numeric order. Each ends with a **GATE** — do not proceed past
   a failing GATE; if [§7 Troubleshooting](#troubleshooting) doesn't cover the failure,
   stop and report rather than improvising.
3. [§1 Discovery](#discovery) produces a table of every mapping pair in the target
   codebase and how it's used. Every later section operates on **that table**, not on
   assumptions about what a typical project looks like.
4. [§3 Migration patterns](#migration-patterns) is the core of the guide: a numbered
   catalog of every AutoMapper feature with the exact replacement code for each. Classify
   every row of your discovery table against this catalog before writing code.
5. Finish with [§8 Final checklist](#final-checklist) as your acceptance gate.

## Table of contents

- [§0 Ground rules](#ground-rules) — scope, hard rules, prerequisites per library tier
- [§1 Discovery](#discovery) — inventory every Profile/CreateMap/IMapper usage · GATE 0
- [§2 Add the library](#add-the-library) — bringing RECO.Mapping in; the pre-C#11 fallback · GATE 1
- [§3 Migration patterns](#migration-patterns) — every AutoMapper feature → its replacement · GATE 2
- [§4 Rewrite call sites](#rewrite-call-sites) — `IMapper` call sites, generic pipelines, DI removal · GATE 3
- [§5 Special cases](#special-cases) — DI-dependent mappings, inheritance, circular references
- [§6 Verification](#verification) — `MappingVerifier` tests per pattern · GATE 4
- [§7 Troubleshooting](#troubleshooting) — error → cause → fix
- [§8 Final checklist](#final-checklist) — acceptance checklist and change-summary template

## Scope boundary — what this guide does NOT do

- It does not upgrade your target framework — that's a separate step, before or after.
- It does not redesign your mapped types — behavioral parity, not a data-model refactor.
- It does not cover mapping libraries other than AutoMapper, though the target pattern
  (explicit compiled code + `MappingVerifier`) is a reasonable replacement for any of them.

---

<a id="ground-rules"></a>

## 0. Ground rules — read completely before doing anything

### 0.1 What "replace AutoMapper" means here, precisely

By the end of this guide, in the target codebase:

- The `AutoMapper` NuGet package (and companions: `AutoMapper.Extensions.Microsoft.
  DependencyInjection`, `AutoMapper.Collection`, etc.) is removed from every project.
- Every `Profile`-derived class and every `CreateMap<>()` configuration is gone.
- Every place that used to call `_mapper.Map<T>(x)`, `_mapper.Map(x, existing)`, or
  `.ProjectTo<T>()` now calls explicit, compiled C# — either a static method implementing
  a `RECO.Mapping` interface, or a plain static/extension method, or (for LINQ
  `ProjectTo` call sites specifically) a compiled `Expression<Func<TSource,TDest>>`.
- Every mapping pair has at least one test built on `RECO.Mapping.Verification.
  MappingVerifier`, proving every destination member is populated (or explicitly
  declared unmapped-by-design) — the direct replacement for AutoMapper's
  `configuration.AssertConfigurationIsValid()`.
- The codebase's **behavior is unchanged**. This is a mechanical translation of existing
  mapping logic into explicit code, not a redesign of what gets mapped or how.

### 0.2 Your codebase is unknown — discover, never assume

This guide is generic on purpose: it has no baseline entity count, no baseline project
count, no assumption about whether mappings are simple or elaborate. Every section after
[§1 Discovery](#discovery) operates on the table *that section* produces for *your*
codebase. Do not skip discovery because "it's probably simple" — AutoMapper's fluent API
makes it trivial to hide a custom value resolver, a conditional map, or a `ProjectTo` call
inside a large `Profile` file; find every one before writing replacement code.

### 0.3 Hard rules (do NOT deviate)

1. **DO NOT** introduce a different mapping library (Mapperly, TinyMapper, a hand-rolled
   reflection-based mapper) as the replacement. The target is `RECO.Mapping` (or, where
   the interface tier doesn't fit, plain hand-written static methods) — full stop.
2. **DO NOT** let a previously-mapped destination member go unpopulated without a
   *conscious, recorded* decision that it's unmapped-by-design. A member that silently
   stops being populated is a behavioral regression, not a refactor.
3. **DO NOT** assume AutoMapper's convention-based member-name matching (including
   "flattening", e.g. `Source.Address.City` → `Dest.AddressCity`) happened the way you'd
   guess. Verify what the old `Profile` actually configured (or, if the `Profile` relied
   purely on convention with no explicit `ForMember`, verify the *destination type's*
   property names against the *source type's* nested structure yourself) before writing
   the explicit replacement. See §3.14 for the exact procedure.
4. **DO NOT** attempt to reuse a compiled mapping method inside an EF Core (or other
   ORM) `IQueryable` LINQ query that used to call `.ProjectTo<T>()`. Compiled method calls
   are opaque to SQL translators; this needs its own pattern — see §3.15 and §5.2.
5. **DO NOT** silently approximate a mapping you can't mechanically translate (a value
   resolver with injected dependencies, `PreserveReferences()`-based cycle handling,
   polymorphic `Include<>()` inheritance mapping). Flag these per [§5](#special-cases)
   and get an explicit decision before writing code for them.
6. **DO** run every GATE in every section. If a GATE fails and
   [§7 Troubleshooting](#troubleshooting) doesn't cover it: stop and report.

### 0.4 Prerequisites — pick your library tier before starting

`RECO.Mapping`'s four interface files (`IMappedFrom`, `IMapsTo`, `IScalarCopyable`,
`IDualMapped`) use **static abstract interface members**, a C# 11 feature requiring the
.NET 7 SDK or later (the *language* version matters, not the target framework runtime —
you can target `net472` with a `net7.0`+ SDK and modern `LangVersion`, but the far more
common case is that your target framework's SDK also determines your available
`LangVersion`). Two tiers:

| Your situation | Tier to use |
| --- | --- |
| Target framework is .NET 7, 8, 9, 10, or any framework buildable with a .NET 7+ SDK and `<LangVersion>11</LangVersion>` or later | **Interface tier** — use all four interfaces as shipped in [`../src/RECO.Mapping/`](../src/RECO.Mapping/). |
| Target framework is .NET 6 or earlier, .NET Standard, or .NET Framework (4.x), and you are not raising the language version independently of the target | **Fallback tier** — skip the four interface files; use plain static/extension methods instead. `MappingVerifier` and `MappingVerificationException` are unaffected by this choice — they use only reflection and delegates, and work identically on both tiers and on every .NET version since .NET Framework 4.5. |

Determine your tier now — [§2](#add-the-library) branches on it immediately. Mixed
solutions (some projects on .NET 8, others still on .NET Framework) may use different
tiers per project; that's fine, the tiers interoperate at the call-site level as long as
each project consistently follows one tier internally.

### 0.5 What "done" looks like

[§8 Final checklist](#final-checklist) is the authoritative acceptance list. In summary:
zero AutoMapper references remain anywhere in the solution; every discovered mapping
pair has explicit code and at least one passing verifier-based test; the solution builds
with no new warnings; and any pre-existing functional/integration tests that exercised
mapped data still pass unmodified (your true behavioral-regression signal —
`MappingVerifier` proves *coverage*, not *correctness of values*).

---

<a id="discovery"></a>

## 1. Discovery — inventory every AutoMapper usage

Run every command below from the solution root. Every later section works from the table
you build at the end of this section — there is no baseline to compare against, because
this guide applies to a codebase this guide has never seen.

### 1.1 Locate the package references

```bash
grep -rln "AutoMapper" --include="*.csproj" .
```

Note every hit — these projects lose their `AutoMapper` (and any `AutoMapper.*`
companion package) reference in [§4.4](#rewrite-call-sites).

### 1.2 Locate every `Profile` class

```bash
grep -rl "using AutoMapper" --include="*.cs" .
grep -rn ": *Profile\b" --include="*.cs" .
```

Open **every** file this finds. A `Profile` constructor is where `CreateMap<>()` chains
live; nothing about their contents is discoverable by grep alone once modifiers
(`.ForMember`, `.Ignore()`, `.ReverseMap()`, `.ConvertUsing()`, `.Condition()`, custom
`IValueResolver`/`ITypeConverter` classes) span multiple lines.

### 1.3 Locate every `CreateMap<>()` call and its modifiers

```bash
grep -rn "CreateMap<" --include="*.cs" .
```

For each hit, read the **full statement** (the fluent chain often continues for several
lines after the `CreateMap<Source, Dest>()` call). Record, per mapping pair:

- Source type, destination type, and which assembly/project each lives in.
- Whether **you own the source** of both types (can you add a `partial` modifier and an
  interface implementation, or is one/both sealed, generated, or from a NuGet package?).
- Every modifier present, classified against this list (full definitions and
  replacements for each are in [§3](#migration-patterns) — this step only needs you to
  *notice and record*, not yet solve):

| Modifier / shape found | Pattern # (see §3) |
| --- | --- |
| No modifiers — plain member-name matching | 3.1 |
| `.ForMember(d => d.X, opt => opt.Ignore())` | 3.2 |
| `.ForMember(d => d.X, opt => opt.MapFrom(s => expr))` | 3.3 |
| `.ForMember(d => d.X, opt => opt.MapFrom<SomeResolver>())` (a class implementing `IValueResolver<,,>`) | 3.4 |
| `.ReverseMap()` | 3.5 |
| `.ConvertUsing(...)` | 3.6 |
| `.ConstructUsing(...)` | 3.7 |
| `.ForMember(..., opt => opt.Condition(...))` | 3.8 |
| `.ForMember(..., opt => opt.NullSubstitute(...))` | 3.9 |
| Called via `_mapper.Map(source, existingInstance)` (two-argument overload) anywhere in the codebase | 3.10 |
| `CreateMap<T, T>()` (identical source/destination type — clone/self-map) | 3.11 |
| Destination has a property whose type is itself a mapped pair (nested object) | 3.12 |
| Destination has a `List<>`/`IEnumerable<>` of a nested mapped pair | 3.13 |
| Destination property name looks like `Source.NestedType.Property` concatenated (e.g. `AddressCity` when `Source.Address.City` exists), with **no** explicit `ForMember` for it | 3.14 — AutoMapper's automatic flattening convention |
| `.Include<Derived, DerivedDto>()` (inheritance/polymorphic mapping) | 5.2 |
| `PreserveReferences()` | 5.3 |

A single `CreateMap<>()` frequently matches several rows at once (e.g. a mapping with
one ignored member, one custom expression, and a nested collection) — record all that
apply.

### 1.4 Locate every call site

```bash
grep -rn "IMapper\b\|_mapper\.\|Mapper\.Map<\|\.ProjectTo<" --include="*.cs" .
```

For each hit, record the file:line, which method it's in, and which mapping pair (from
§1.3) it invokes. **Every call site must trace back to a row in your §1.3 table** — if
one doesn't, the corresponding `CreateMap<>()` is either missing from your inventory or
the call resolves a mapping via an interface/base-type registered elsewhere; keep
looking until every call site is accounted for.

Separately flag:
- Call sites using the **two-argument** `Map(source, existingInstance)` overload — these
  are in-place updates (pattern 3.10), a different shape than construction.
  `_mapper.Map<TDest>(source)` and `_mapper.Map(source, existing)` are NOT the same
  pattern even for the same `TSource`/`TDest` pair — check the argument count.
- Call sites inside a `.Select(...)` or directly as `.ProjectTo<TDest>()` on an
  `IQueryable<TSource>` — these are LINQ-translated projections (pattern 3.15),
  fundamentally different from everything else in this guide.
- Generic pipeline/service classes parameterized by `<TSource,TDest>` (or similar) that
  take `IMapper` as a constructor dependency and call it internally with type parameters
  rather than concrete types — these need the generic-constraint or delegate-injection
  treatment in [§4.3](#rewrite-call-sites), not a simple one-line replacement.

### 1.5 Locate the DI registration and configuration validation

```bash
grep -rn "AddAutoMapper\|AssertConfigurationIsValid\|MapperConfiguration" --include="*.cs" .
```

Record every hit — `AddAutoMapper(...)` registrations are removed in
[§4](#rewrite-call-sites); `AssertConfigurationIsValid()` test calls are replaced
one-for-one by `MappingVerifier` tests in [§6](#verification).

### 1.6 Locate global configuration options

Inside the same `MapperConfiguration`/`Profile` files, check for options that apply
across *every* mapping rather than to one pair specifically:

```bash
grep -rn "AllowNullCollections\|AllowNullDestinationValues\|ForAllMaps\|ForAllPropertyMaps\|SourceMemberNamingConvention\|DestinationMemberNamingConvention" --include="*.cs" .
```

Record any hits — see §3.16 for how to re-derive their effect explicitly per pair.

### Your discovery deliverable

A table with one row per mapping pair (source type, destination type, project/assembly
of each, own-both-types? yes/no, list of pattern #s from §1.3, list of call sites from
§1.4 with file:line, in-place-update usage? yes/no, ProjectTo usage? yes/no). This table
is what [§3](#migration-patterns) and [§6](#verification) are executed against.

### ✅ GATE 0

- Every `CreateMap<>()` found in §1.3 has a row in your table.
- Every call site found in §1.4 traces to exactly one row (or is itself flagged as a
  generic pipeline needing special handling per §1.4's last bullet).
- Every global option found in §1.6 is recorded against the pairs it affects (all pairs,
  if truly global).
- You know, for every row, whether you own both mapped types' source code.

If any of the above is incomplete, do not proceed — an incomplete inventory means a
later section will silently miss a mapping.

---

<a id="add-the-library"></a>

## 2. Add RECO.Mapping to your project

You determined your tier in [§0.4](#ground-rules). Both tiers start the same way, then
diverge.

### 2.1 Bring in the source

Copy the folder [`../src/RECO.Mapping/`](../src/RECO.Mapping/) from this repository into
your solution — e.g. `src/Common/RECO.Mapping/` if you have a shared-libraries
convention, or directly inside an existing project's folder if you'd rather not add a
new project (both work; the code has zero package dependencies either way). The 7 files:

```
RECO.Mapping.csproj
IMappedFrom.cs
IMapsTo.cs
IScalarCopyable.cs
IDualMapped.cs
MappingExtensions.cs
Verification/MappingVerificationException.cs
Verification/MappingVerifier.cs
```

If added as its own project, wire it into the solution and reference it from every
project that used AutoMapper:

```bash
dotnet sln <your-solution>.sln add path/to/RECO.Mapping/RECO.Mapping.csproj
dotnet add <each consuming project>.csproj reference path/to/RECO.Mapping/RECO.Mapping.csproj
```

### 2.2 Interface tier (C# 11 / .NET 7+) — use everything as-is

Retarget the copied `RECO.Mapping.csproj`'s `<TargetFramework>` from `net10.0` to match
your project's actual target framework (e.g. `net8.0`, `net9.0`) — everything else in
the file (`Nullable`, `ImplicitUsings`, `LangVersion latest`) is framework-agnostic and
should stay as shipped. No other file needs modification. Proceed to
[§3](#migration-patterns).

### 2.3 Fallback tier (pre-C# 11 / .NET Framework / .NET Standard) — swap 5 files

Static abstract interface members do not compile below C# 11. Do the following instead:

1. **Delete** (or simply don't add) these four files — they will not compile on your
   toolchain: `IMappedFrom.cs`, `IMapsTo.cs`, `IScalarCopyable.cs`, `IDualMapped.cs`.
2. **Replace** `MappingExtensions.cs` — the shipped version's generic constraints
   reference the interfaces you just removed. Use this delegate-based version instead
   (verified to compile and run standalone on `net10.0`; behaves identically on earlier
   frameworks since it uses nothing beyond basic LINQ):

   ```csharp
   namespace RECO.Mapping;

   /// <summary>
   /// Collection helpers over plain mapping delegates. Fallback-tier equivalent of the
   /// interface-based MapFromAll/MapToAll — takes the mapping function explicitly
   /// instead of resolving it through a static interface member, so it works on any
   /// C# version and with types you don't own.
   /// </summary>
   public static class MappingExtensions
   {
       /// <summary>Maps every element of <paramref name="sources"/> with <paramref name="map"/>.</summary>
       public static List<TDest> MapAll<TSource, TDest>(
           this IEnumerable<TSource> sources, Func<TSource, TDest> map)
           => sources.Select(map).ToList();
   }
   ```

   Call sites become `sources.MapAll(PersonMapper.ToDto)` instead of
   `sources.MapFromAll<TDest, TSource>()`. Functionally identical; just an explicit
   delegate parameter instead of a static-interface-member lookup.
3. **Keep unchanged**: `RECO.Mapping.csproj` (retarget `<TargetFramework>` to match your
   project — `<Nullable>`/`<ImplicitUsings>` may need to become `disable`/`false` if
   your target predates their availability, e.g. .NET Framework or old .NET Standard;
   check your other projects' csproj files for the convention already in use),
   `Verification/MappingVerificationException.cs`, and
   `Verification/MappingVerifier.cs` — **neither verification file uses static abstract
   members**; they use only reflection (`System.Reflection`) and a plain `Func<,>`
   delegate, both available since .NET Framework 4.5. This is the one piece of the
   library every tier gets identically.

Every mapping pair in this tier is written as plain static methods (see [§3](#migration-patterns),
which gives both tiers' code side by side for each pattern) — a static class per pair, or
one static class per source project holding all its mapping methods as extension
methods, whichever matches your codebase's existing conventions.

### ✅ GATE 1

```bash
dotnet build path/to/RECO.Mapping/RECO.Mapping.csproj
```

builds with 0 errors, using whichever variant (interface tier unmodified, or fallback
tier with the 4 interfaces removed and `MappingExtensions.cs` replaced) matches your
prerequisites from §0.4. If it doesn't build, recheck: target framework actually supports
your chosen tier; `LangVersion` is explicit and high enough for the interface tier;
`Nullable`/`ImplicitUsings` settings match what your target framework accepts.

---

<a id="migration-patterns"></a>

## 3. Migration patterns — every AutoMapper feature and its exact replacement

Work through your [discovery](#discovery) table row by row. For each row, apply every
pattern number recorded against it, in the order given below when more than one applies
to the same pair (nested/collection patterns, §3.12–3.13, are usually applied *around*
whichever base pattern §3.1–3.11 the pair's top-level shape uses).

Each pattern gives **both library tiers' code** where the tier changes the shape; where
it doesn't (most of §3.2–§3.9 — these are about what goes *inside* a mapping method, not
the method's shape), one example suffices for both.

### 3.1 Simple map — `CreateMap<Source, Dest>()`, no modifiers

Every property is copied by matching name. Write every assignment explicitly — do not
rely on the compiler or a helper to "match names for you"; that reflection-based
convenience is exactly what AutoMapper provided and exactly what you're removing.

**Interface tier** (you own `Dest`'s source):

```csharp
public sealed class PersonDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public sealed class PersonEntity : IMappedFrom<PersonEntity, PersonDto>
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public static PersonEntity MapFrom(PersonDto source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        LastName = source.LastName,
    };
}

// call site: var entity = PersonEntity.MapFrom(dto);
```

**Fallback tier** (or you don't own either type — e.g. `PersonEntity` is sealed/generated):

```csharp
public static class PersonMapper
{
    public static PersonEntity ToEntity(this PersonDto source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        LastName = source.LastName,
    };
}

// call site: var entity = dto.ToEntity();
```

Both are equally valid replacements; the interface tier only buys you generic-constraint
call sites in pipeline code (§4.3) — pick per-pair based on whether you own the type and
whether anything downstream needs the generic constraint.

### 3.2 Ignored member — `.ForMember(d => d.X, opt => opt.Ignore())`

Simply don't assign `X` in the new method. Record `nameof(Dest.X)` — you will pass it as
`unmappedByDesign` to `MappingVerifier.AssertAllMembersMapped` in [§6](#verification), so
the test proves it was *intentionally* skipped rather than *forgotten*.

### 3.3 Custom member expression — `.ForMember(d => d.X, opt => opt.MapFrom(s => expr))`

Inline `expr` as `X`'s assignment:

```csharp
FullName = $"{source.FirstName} {source.LastName}",
```

If `expr` is non-trivial, extract a small private static helper in the same mapping
class rather than a one-liner — keep it in the same assembly, no runtime indirection.

### 3.4 Value resolver class — `.ForMember(d => d.X, opt => opt.MapFrom<SomeResolver>())`

Find `SomeResolver`'s `Resolve(source, destination, destMember, context)` method body.
If it has **no constructor dependencies**, inline its logic exactly like §3.3. If it
**does** have injected dependencies (a lookup service, current-user accessor, clock,
etc.), this mapping can no longer be a static method — see [§5.1](#special-cases)
(DI-dependent mapping services).

### 3.5 `ReverseMap()`

Implement both directions.

**Interface tier:**

```csharp
public sealed class PersonEntity : IDualMapped<PersonEntity, PersonDto>
{
    // ... properties ...

    public static PersonEntity MapFrom(PersonDto source) => new() { /* ... */ };
    public PersonDto MapTo() => new() { /* ... */ };
}
```

`IDualMapped<TSelf,TOther>` is exactly `IMappedFrom<TSelf,TOther> + IMapsTo<TOther>` —
use it whenever both directions are needed instead of implementing them separately.

**Fallback tier:** two extension methods, one per direction, in the same static class:

```csharp
public static class PersonMapper
{
    public static PersonEntity ToEntity(this PersonDto source) => new() { /* ... */ };
    public static PersonDto ToDto(this PersonEntity entity) => new() { /* ... */ };
}
```

If AutoMapper's `ReverseMap()` had its **own** additional `ForMember`/`Ignore` modifiers
for the reverse direction (this is allowed and common — e.g. `A→B` maps a computed field
that `B→A` can't reconstruct and must ignore), apply those modifiers **only** to that
direction's method, not both.

### 3.6 `ConvertUsing(...)` — whole-type custom conversion

AutoMapper skips member-by-member mapping entirely and calls your conversion function.
This is the simplest case in disguise — write `MapFrom`/`MapTo`/`ToEntity`/`ToDto` (per
your tier) with a single expression matching what the `ConvertUsing` lambda or
`ITypeConverter` did. There is nothing else to translate.

### 3.7 `ConstructUsing(...)` — custom construction, then normal member mapping continues

Put the custom construction expression in the `new(...)` call itself, then continue with
the usual per-property assignments in the object initializer for whatever
`ConstructUsing` left for AutoMapper to fill afterward:

```csharp
public static PersonEntity MapFrom(PersonDto source) => new PersonEntity(source.Id) // custom ctor call
{
    FirstName = source.FirstName, // normal member mapping continues
    LastName = source.LastName,
};
```

### 3.8 `.Condition(...)` — member mapped only if a predicate holds

Two distinct situations, tell them apart before choosing:

- **Constructing a new instance** (the common case): inline as a conditional expression.
  ```csharp
  Discount = source.IsPremiumMember ? source.Discount : 0m,
  ```
- **Updating an existing instance** where the condition's purpose is "leave the existing
  value alone if the condition is false" (the destination already has a value that must
  be *preserved*, not defaulted): this is actually the in-place-update pattern — see
  §3.10 and put the conditional inside `CopyScalarsTo`'s body instead:
  ```csharp
  public void CopyScalarsTo(PersonEntity target)
  {
      if (IsPremiumMember) target.Discount = Discount; // else target.Discount is untouched
      target.FirstName = FirstName; // unconditional members still copy normally
  }
  ```

### 3.9 `.NullSubstitute(...)`

Inline as a null-coalescing expression:

```csharp
DisplayName = source.PreferredName ?? "Anonymous",
```

### 3.10 In-place update — `_mapper.Map(source, existingDestination)` (two-argument overload)

This is `IScalarCopyable<T>.CopyScalarsTo` — updating an already-existing/tracked
instance without replacing it or disturbing its navigation/relationship properties
(very likely why the original code used the two-argument overload instead of
construction: to keep an EF Core change-tracked entity intact).

**Interface tier:**

```csharp
public sealed class PersonEntity : IScalarCopyable<PersonEntity>
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public List<OrderEntity> Orders { get; set; } = []; // navigation — must NOT be touched

    public PersonEntity CloneScalars() => new() { Id = Id, FirstName = FirstName };

    public void CopyScalarsTo(PersonEntity target)
    {
        target.Id = Id;
        target.FirstName = FirstName;
        // target.Orders is intentionally left untouched.
    }
}

// call site (was: _mapper.Map(incoming, tracked)):
incoming.CopyScalarsTo(tracked);
```

**Fallback tier:**

```csharp
public static class PersonMapper
{
    public static PersonEntity CloneScalars(this PersonEntity source) => new()
    {
        Id = source.Id, FirstName = source.FirstName,
    };

    public static void CopyScalarsTo(this PersonEntity source, PersonEntity target)
    {
        target.Id = source.Id;
        target.FirstName = source.FirstName;
        // target.Orders is intentionally left untouched.
    }
}
```

If the old code also snapshotted "before" state prior to overwriting
(`var old = _mapper.Map<T>(existing);` immediately before the in-place `Map(...)` call),
that snapshot call is `CloneScalars()`.

### 3.11 Self-mapping / deep clone — `CreateMap<T, T>()`, `_mapper.Map<T>(instance)` where source == destination type

If this was used to snapshot scalar state, it's `CloneScalars()` from §3.10. If a
genuine **deep** clone (including navigation/collections) is actually required, write an
explicit manual clone instead of trying to force it through the scalar-only contract:

```csharp
public PersonEntity DeepClone() => new()
{
    Id = Id, FirstName = FirstName,
    Orders = [.. Orders.Select(o => o.DeepClone())],
};
```

For `record` types, prefer a `with` expression for the shallow case
(`instance with { }`, or `instance with { FirstName = newName }` for a modified copy) —
records already give you this for free; don't wrap it in a mapping method needlessly.

### 3.12 Nested / complex-type member (destination property is itself a mapped pair)

Give the nested pair its own mapping first (leaf types before parents — work through
your discovery table in dependency order, innermost types first), then the parent
mapping calls it directly:

```csharp
// AddressDto/AddressEntity already have their own MapFrom/MapTo from §3.1 or §3.5.
public static PersonEntity MapFrom(PersonDto source) => new()
{
    Id = source.Id,
    Address = source.Address is null ? null : AddressEntity.MapFrom(source.Address),
    // fallback tier: Address = source.Address?.ToEntity(),
};
```

### 3.13 Collection of a nested mapped pair

Use the library's list helpers after the nested pair has its own mapping (§3.12):

```csharp
// Interface tier:
Lines = source.Lines.MapFromAll<LineEntity, LineDto>(),

// Fallback tier:
Lines = source.Lines.MapAll(LineMapper.ToEntity),
```

### 3.14 Automatic member-name "flattening" — no explicit `ForMember`, but the property name pattern-matches a nested source path

**This is the single most common source of a silent regression in this kind of
migration.** AutoMapper's convention engine maps `Source.Address.City` to
`Dest.AddressCity` (and similar `Foo.Bar` → `FooBar` patterns) with **zero explicit
configuration** — nothing in the `Profile` file tells you this is happening. It compiles
fine either way; a forgotten flattened property just silently stays at its default value
at runtime, which is very easy to miss in review.

**Procedure:** for every destination type, list every property name. For each one that
doesn't correspond to a same-named source property, check whether it matches a
concatenation of the source type's *nested* property paths (case-insensitively,
ignoring word boundaries — AutoMapper's convention is fuzzy). If it does, write the
explicit expression:

```csharp
AddressCity = source.Address?.City ?? "",
AddressPostalCode = source.Address?.PostalCode ?? "",
```

Do this check for **every** destination property in **every** mapping pair from your
discovery table, not just the ones with obvious naming — it's exactly the mappings that
look "simple" (§3.1, no explicit `ForMember` at all) that are most likely relying
entirely on this invisible convention.

### 3.15 `ProjectTo<TDest>()` on an `IQueryable<TSource>` — LINQ-to-SQL projection

**Fundamentally different from every other pattern in this guide.** `ProjectTo` builds
an expression tree that EF Core's (or another ORM's) query provider translates to SQL —
it runs *before* materialization, inside the database. A compiled method call
(`MapFrom`/`ToEntity`/anything from §3.1–3.13) is opaque to that translator; calling it
inside a `.Select()` throws `InvalidOperationException: could not be translated` or
silently forces full materialization first (a serious, easy-to-miss performance
regression — pulling the whole table into memory before filtering).

**Replacement:** an explicit `.Select()` projection written directly in the query:

```csharp
// was: query.ProjectTo<PersonDto>(mapperConfig)
var results = query.Select(p => new PersonDto
{
    Id = p.Id,
    FullName = p.FirstName + " " + p.LastName, // SQL-translatable string concat
}).ToList();
```

If the same projection shape is used in multiple queries, extract a static
`Expression<Func<TSource,TDest>>` (an expression tree, not a compiled delegate) so every
call site shares one definition and one test (see §6):

```csharp
public static class PersonProjections
{
    public static Expression<Func<Person, PersonDto>> ToDto => p => new PersonDto
    {
        Id = p.Id,
        FullName = p.FirstName + " " + p.LastName,
    };
}

// call site: query.Select(PersonProjections.ToDto)
```

Only use expressions EF Core (or your ORM) can translate to SQL inside the projection
body — no calls to arbitrary C# methods, no string formatting helpers beyond what the
provider explicitly supports. If a projection genuinely needs code that can't run in
SQL, materialize first (`.AsEnumerable()`/`.ToList()`) then map in memory with the normal
compiled-method patterns — accept that performance tradeoff consciously rather than by
accident.

### 3.16 Global configuration options

`AllowNullCollections`, `AllowNullDestinationValues`, `ForAllMaps(...)`,
`ForAllPropertyMaps(...)`, naming-convention settings — these apply invisibly to *every*
mapping in the profile/configuration, not just one pair. Re-derive their effect
explicitly, per pair, wherever they'd have mattered. For example, if
`AllowNullCollections = true` was set (meaning a null source collection maps to an empty
destination collection rather than a null one — AutoMapper's default is actually the
reverse, `false`, so check which your codebase had):

```csharp
Items = source.Items?.Select(i => LineEntity.MapFrom(i)).ToList() ?? [],
```

Don't assume "it just works" — write out what the global option actually did, once per
affected pair.

### Quick-reference: trigger → pattern

| What you found in the old `CreateMap<>()` chain | Apply |
| --- | --- |
| Nothing (bare `CreateMap<A,B>()`) | §3.1, then check §3.14 for hidden flattening |
| `.Ignore()` | §3.2 |
| `.MapFrom(s => expr)` | §3.3 |
| `.MapFrom<Resolver>()` | §3.4 (→ §5.1 if the resolver has DI dependencies) |
| `.ReverseMap()` | §3.5 |
| `.ConvertUsing(...)` | §3.6 |
| `.ConstructUsing(...)` | §3.7 |
| `.Condition(...)` | §3.8 |
| `.NullSubstitute(...)` | §3.9 |
| `Map(source, existing)` two-arg call site | §3.10 |
| `CreateMap<T,T>()` | §3.11 |
| Nested object property | §3.12 |
| Nested collection property | §3.13 |
| Property name looks flattened, no `ForMember` | §3.14 |
| `.ProjectTo<T>()` / `IQueryable` `.Select` misuse | §3.15 |
| `AllowNull*`/`ForAllMaps`/naming convention settings | §3.16 |
| `.Include<Derived,DerivedDto>()` | §5.2 |
| `PreserveReferences()` | §5.3 |

### ✅ GATE 2

Every row in your discovery table has explicit replacement code written for every
pattern # recorded against it. Every destination property in every pair has been
explicitly assigned somewhere, or is recorded as unmapped-by-design (§3.2's list). Every
§3.14 candidate has been checked against the source type's nested structure, not assumed
either way.

---

<a id="rewrite-call-sites"></a>

## 4. Rewrite call sites and remove AutoMapper's DI wiring

With every mapping pair's replacement code written (§3), work through every call site
recorded in [discovery](#discovery) §1.4.

### 4.1 Simple construction call sites

| Old | New (interface tier) | New (fallback tier) |
| --- | --- | --- |
| `_mapper.Map<TDest>(source)` | `source.MapTo()` or `TDest.MapFrom(source)` (either is correct — see below) | `source.ToDest()` / `DestMapper.FromSource(source)` |
| `_mapper.Map<List<TDest>>(sources)` | `sources.MapFromAll<TDest,TSource>()` | `sources.MapAll(DestMapper.FromSource)` |

When both `MapTo()` and `TDest.MapFrom(source)` are available for the same pair (i.e.
you implemented `IDualMapped`), pick whichever reads more naturally at each call site:
`source.MapTo()` when the call site already holds a `source` instance and is projecting
it outward; `TDest.MapFrom(source)` when the call site is constructing a `TDest` and the
source is just an input (e.g. inside a repository's insert method). Consistency within
one file matters more than which you pick.

### 4.2 In-place update call sites

| Old | New (interface tier) | New (fallback tier) |
| --- | --- | --- |
| `_mapper.Map(source, existingInstance)` | `source.CopyScalarsTo(existingInstance)` | `SourceMapper.CopyScalarsTo(source, existingInstance)` |
| `var old = _mapper.Map<T>(existingInstance);` (pre-update snapshot) | `var old = existingInstance.CloneScalars();` | `var old = existingInstance.CloneScalars();` (extension method, fallback tier) |

### 4.3 Generic pipeline / service classes parameterized by `<TSource, TDest>`

If discovery (§1.4) flagged a class like:

```csharp
public class ActionProcessor<TDataModel, TDbModel>
{
    public ActionProcessor(IMapper mapper, /* ... */) { _mapper = mapper; }
    // internally calls _mapper.Map<TDbModel>(x), _mapper.Map<TDataModel>(y), etc.
}
```

this needs one of two treatments depending on whether you own both `TDataModel` and
`TDbModel`'s source for *every* instantiation of this generic class in the codebase:

**You own both type families (interface tier available everywhere this class is used):**
add the interface as a generic constraint on the class itself — the mapping is now
resolved at compile time per closed generic type, with no injected mapper at all:

```csharp
public class ActionProcessor<TDataModel, TDbModel>
    where TDbModel : IDualMapped<TDbModel, TDataModel>
{
    // no IMapper constructor parameter anymore
    private void Save(TDataModel data)
    {
        TDbModel dbModel = TDbModel.MapFrom(data);
        // ...
    }

    private TDataModel Load(TDbModel dbModel) => dbModel.MapTo();
}
```

**You don't own the types everywhere this class is used** (fallback tier, or some
instantiations use third-party/sealed types): replace the injected `IMapper` with two
plain delegates supplied by the caller — this preserves the "pluggable mapping"
flexibility AutoMapper's runtime `IMapper` gave you, but each delegate is a concrete
compiled method reference resolved at the composition root, not a runtime type lookup:

```csharp
public class ActionProcessor<TDataModel, TDbModel>
{
    private readonly Func<TDataModel, TDbModel> _toDbModel;
    private readonly Func<TDbModel, TDataModel> _toDataModel;

    public ActionProcessor(Func<TDataModel, TDbModel> toDbModel, Func<TDbModel, TDataModel> toDataModel, /* ... */)
    {
        _toDbModel = toDbModel;
        _toDataModel = toDataModel;
    }

    private void Save(TDataModel data) { TDbModel dbModel = _toDbModel(data); /* ... */ }
    private TDataModel Load(TDbModel dbModel) => _toDataModel(dbModel);
}

// composition root / DI registration, one line per concrete type pair:
services.AddScoped<ActionProcessor<PersonDto, PersonEntity>>(sp =>
    new ActionProcessor<PersonDto, PersonEntity>(PersonMapper.ToEntity, PersonMapper.ToDto, /* ... */));
```

Either way, **every previously-dynamic mapping call becomes visible and compile-time
checked at its instantiation site** — this is a genuine improvement AutoMapper's
`IMapper` (which resolves any `Map<T>()` call at runtime against its whole
configuration) could not offer, but it does mean each generic instantiation needs an
explicit one-line wiring instead of "it just works because AutoMapper knows about it".

### 4.4 Remove AutoMapper's DI registration and package references

```bash
# For every project found in discovery §1.1:
dotnet remove <path-to-csproj> package AutoMapper
dotnet remove <path-to-csproj> package AutoMapper.Extensions.Microsoft.DependencyInjection  # if present
```

Delete the `services.AddAutoMapper(...)` call (found in discovery §1.5) and every
`Profile`-derived class — their contents are now fully absorbed into the pattern-specific
mapping methods from §3. If a `Profile` class still compiles after deletion of its
`AutoMapper` using directive, that's a sign something inside it wasn't actually
AutoMapper-specific (rare, but check before deleting the file outright).

### ✅ GATE 3

```bash
grep -rn "AutoMapper\|IMapper\b" --include="*.cs" --include="*.csproj" .
```

returns **nothing** in your target codebase (matches inside this guide's own files, if
you copied it alongside your project, don't count). Every call site from discovery §1.4
now calls explicit code. Every generic pipeline class from §1.4's last bullet has been
converted per §4.3.

---

<a id="special-cases"></a>

## 5. Special cases — slow down and think, don't mechanically apply

The patterns in [§3](#migration-patterns) are mechanical translations. The cases below
are not — each requires a design decision. Flag these to whoever owns the codebase
rather than guessing; a wrong guess here is silent and expensive precisely because it
usually still compiles and often still runs.

### 5.1 DI-dependent mappings (custom value resolver with injected dependencies)

A value resolver that needs a service — a code-lookup table, the current user, a clock,
an external API client — cannot become a static method (§3.4 assumed no dependencies;
this is the case where it has some). Convert it to an **injectable mapping service**
instead:

```csharp
public sealed class PersonMappingService(ICodeLookupService lookup, TimeProvider clock)
{
    public PersonDto ToDto(PersonEntity entity) => new()
    {
        Id = entity.Id,
        CountryName = lookup.NameFor(entity.CountryCode),
        SnapshotTakenUtc = clock.GetUtcNow(),
    };
}
```

Register it in DI at the same lifetime its dependencies need (usually `Scoped`, matching
`ICodeLookupService`'s own lifetime — check, don't assume). It's still verified the same
way as everything else (see [§6](#verification)) by constructing the service with test
doubles for its dependencies and passing its method as the `Func<TSource,TDest>`:

```csharp
var service = new PersonMappingService(fakeLookup, fakeClock);
MappingVerifier.AssertAllMembersMapped<PersonEntity, PersonDto>(service.ToDto);
```

This is the correct outcome — not every mapping can be a pure static function, and
`MappingVerifier` doesn't require one; it only needs a delegate.

### 5.2 Inheritance / polymorphic mapping — `.Include<Derived, DerivedDto>()`

AutoMapper resolves the concrete runtime type automatically at map time. There is no
equivalent automatic mechanism here — write one mapping method per concrete pair (a
`DerivedDto.MapFrom(Derived)` alongside the base `BaseDto.MapFrom(Base)`), then dispatch
explicitly at the call site with a type switch:

```csharp
BaseDto dto = source switch
{
    Derived1 d1 => Derived1Dto.MapFrom(d1),
    Derived2 d2 => Derived2Dto.MapFrom(d2),
    _ => BaseDto.MapFrom(source),
};
```

If new derived types are added over time, this switch is the one place that must be
kept in sync — consider a test that enumerates all subtypes of `Base` via reflection
(test-time only, mirroring how `MappingVerifier` uses reflection) and asserts each has a
case in the switch, so a forgotten case fails a test instead of silently falling through
to the base mapping at runtime.

### 5.3 Circular / self-referencing object graphs — `PreserveReferences()`

AutoMapper's `PreserveReferences()` detects reference cycles at runtime and reuses the
already-mapped instance instead of recursing infinitely. Hand-written recursive
`MapFrom`/`MapTo` calls have no such protection — a true cycle (`Person.Manager.Reports`
containing the original `Person` again) will stack-overflow.

This needs a genuine design decision, not a mechanical fix:
- **Most common resolution:** map the back-reference as an identifier only
  (`ManagerId = source.Manager?.Id`), not the full nested object. This is usually what
  the data actually needs and avoids the cycle entirely.
- **If the full nested object is genuinely required** in both directions: cap the
  recursion depth explicitly (an optional `int depth = 0` parameter threaded through,
  returning `null`/an ID-only stub past a fixed limit), or maintain your own
  already-visited set for the duration of one mapping call (a `Dictionary<object,object>`
  identity map passed through recursive calls) — essentially reimplementing what
  `PreserveReferences()` did, deliberately, because your codebase turned out to need it.

Flag every `PreserveReferences()` hit from discovery and resolve each one individually —
don't apply a blanket pattern across all of them; the right answer depends on what the
cyclic data actually means in each case.

### 5.4 Global naming conventions beyond flattening

If discovery (§1.6) found `SourceMemberNamingConvention`/`DestinationMemberNamingConvention`
set to something other than the default (e.g. mapping `snake_case` source properties —
common when the source model came from a JSON API or a legacy database — to `PascalCase`
destination properties), this is a **more aggressive** version of the flattening problem
in §3.14: property name matching happened via a convention transform, not identity. Treat
every destination property as needing explicit verification against the source's actual
member names (not just its structure) — the naming convention makes the "obviously
matches" heuristic in §3.14 unreliable, so check every property, not just the
suspicious-looking ones.

---

<a id="verification"></a>

## 6. Verification — one test per mapping pair, per direction

`MappingVerifier.AssertAllMembersMapped<TSource,TDest>(map, params unmappedByDesign)` is
the direct replacement for AutoMapper's `configuration.AssertConfigurationIsValid()`. It
takes any `Func<TSource,TDest>` — it does not require `TSource`/`TDest` to implement any
`RECO.Mapping` interface, so it works identically for both library tiers and for
DI-dependent mapping services (§5.1). Add the test project's reference to `RECO.Mapping`
(and, if separate, `RECO.Mapping.Verification`'s namespace — it lives in the same
project) alongside your existing test framework (NUnit, xUnit, MSTest — the verifier
throws a plain exception on failure and doesn't care which framework asserts on it).

### 6.1 The basic test shape

One test per **direction**, per pair — construction and, if `ReverseMap`/`IDualMapped`
is used, the reverse projection too:

```csharp
[Test]
public void PersonDto_maps_to_PersonEntity_completely()
{
    MappingVerifier.AssertAllMembersMapped<PersonDto, PersonEntity>(
        PersonEntity.MapFrom,                  // or dto => dto.ToEntity() on the fallback tier
        /* unmappedByDesign: */ nameof(PersonEntity.Tags));
}

[Test]
public void PersonEntity_maps_to_PersonDto_completely()
{
    MappingVerifier.AssertAllMembersMapped<PersonEntity, PersonDto>(e => e.MapTo());
}
```

The `unmappedByDesign` list **must exactly match** the members you recorded as ignored
in [§3.2](#migration-patterns) for that pair and direction — cross-check against your
discovery table. A mismatch here is the test catching either a forgotten mapping or a
stale ignore-list entry; see [§7](#troubleshooting) for both failure messages.

### 6.2 In-place update pairs (§3.10)

Wrap `CopyScalarsTo` as a construction-shaped delegate for the verifier (which always
maps `TSource → TDest` by construction) by copying onto a fresh baseline instance:

```csharp
[Test]
public void CopyScalarsTo_covers_every_scalar_member()
{
    MappingVerifier.AssertAllMembersMapped<PersonEntity, PersonEntity>(
        source => { var target = new PersonEntity(); source.CopyScalarsTo(target); return target; },
        nameof(PersonEntity.Orders)); // the navigation property CopyScalarsTo must NOT touch
}
```

This simultaneously proves two things: every scalar is covered (main assertion) and the
navigation property is untouched (it's in `unmappedByDesign`, so the verifier confirms
it stayed at its constructor default rather than picking up the source's value).

### 6.3 DI-dependent mapping services (§5.1)

Construct the service with test doubles, pass its instance method:

```csharp
[Test]
public void PersonMappingService_covers_every_member()
{
    var service = new PersonMappingService(new FakeCodeLookupService(), FakeTimeProvider());
    MappingVerifier.AssertAllMembersMapped<PersonEntity, PersonDto>(service.ToDto);
}
```

### 6.4 `ProjectTo`-replaced LINQ expressions (§3.15)

`MappingVerifier` takes a compiled delegate, not an expression tree — compile the
expression inside the test to prove the *projection definition* is complete (this does
not test SQL translation; that's what your existing integration tests against a real or
in-memory provider are for):

```csharp
[Test]
public void PersonProjection_covers_every_destination_member()
    => MappingVerifier.AssertAllMembersMapped<Person, PersonDto>(PersonProjections.ToDto.Compile());
```

### 6.5 Inheritance dispatch (§5.2)

Add one coverage test per concrete derived pair (each `DerivedDto.MapFrom(Derived)` is
just another mapping pair, tested exactly like §6.1), plus — if you built one — the
reflection-based "every subtype has a switch case" test suggested in §5.2.

### ✅ GATE 4 — acceptance

1. Every row in your [discovery](#discovery) table has at least one `MappingVerifier`-based
   test (both directions, if bidirectional).
2. All such tests pass.
3. Every `unmappedByDesign` list matches the ignored-members list you recorded in §3.2,
   pair for pair, direction for direction.
4. Every §3.14 flattened property and every §1.4 `ProjectTo` call site was explicitly
   handled — check this against your discovery table one more time; a verifier test
   only proves what it was told to check, not that you didn't forget an entire row.
5. Full solution builds with no new warnings introduced by this migration.
6. Any pre-existing functional/integration tests that exercised mapped data end-to-end
   still pass, unmodified — this is your genuine behavioral-regression signal.
   `MappingVerifier` proves *coverage* (every member got assigned something), not
   *correctness* (the right value) — that's what these existing tests, and manual review
   of every §3.3/§3.4/§3.14 custom expression against the original AutoMapper
   configuration, are for.

---

<a id="troubleshooting"></a>

## 7. Troubleshooting — error → cause → fix

| Symptom | Cause | Fix |
| --- | --- | --- |
| `CS8920`/`CS8929`/"static abstract members require C# 11 or greater" | Interface tier used on a target framework/SDK below the requirement | Switch that project to the fallback tier — [§2.3](#add-the-library) |
| `CS0311: cannot be used as type parameter 'TDbModel'` on a generic pipeline class | The class is constrained on an interface (`IDualMapped<...>`, etc.) but a type used with it doesn't implement it | Either implement the interface on that type, or convert that pipeline to the delegate-based fallback — [§4.3](#rewrite-call-sites) |
| `CS0535: does not implement interface member 'MapFrom'` | Signature mismatch — must be exactly `public static TSelf MapFrom(TSource source)` | Match the interface's exact static-abstract signature |
| `MappingVerificationException: 'X' was not populated` | A property assignment was missed, or `X` is actually a flattened member (§3.14) needing an explicit nested-path expression | Recheck the property against the source type's structure; add the missing assignment |
| `MappingVerificationException: 'X' is declared unmapped-by-design but received the source value` | Either `X` shouldn't be in the `unmappedByDesign` list (it should be mapped), or it's being accidentally assigned somewhere despite being ignored | Cross-check against your §3.2 ignore-list record for this pair; fix whichever side is wrong |
| Runtime data is wrong for a specific property, but build succeeded and the verifier test passed | The property is covered by *some* expression, but not the *correct* one — the verifier only proves "not left at default," not "matches the old value" | Compare against the original `Profile`'s exact `ForMember`/flattening logic for that property; write (or run) a characterization test against the OLD AutoMapper-based code's output *before* removing it, if you haven't already captured that behavior during discovery |
| `InvalidOperationException: ... could not be translated` on a query that used to call `.ProjectTo<T>()` | The hand-written `.Select()` replacement (§3.15) calls something the ORM's query provider can't translate to SQL | Keep the projection to pure, provider-translatable expressions only; move anything requiring arbitrary C# execution to after `.ToList()`/`.AsEnumerable()`, accepting the performance tradeoff consciously |
| EF Core (or another ORM) throws a duplicate-tracking / "entity already tracked" exception after replacing an in-place update | A fresh instance was *constructed* (`MapFrom`) where the code actually needs to *update* an already-tracked instance (`CopyScalarsTo`) | Recheck the call site's original AutoMapper overload — one-argument `Map<T>()` was construction, two-argument `Map(source, existing)` was in-place update; these are not interchangeable — §3.10 |
| Stack overflow mapping a graph that used to work under AutoMapper | AutoMapper's `PreserveReferences()` was silently protecting a reference cycle the hand-written recursive mapping doesn't guard against | §5.3 — map the back-reference as an ID, or add explicit cycle/depth protection |
| A concrete derived type's data is missing after switching from `.Include<>()` inheritance mapping | The type-switch dispatch (§5.2) doesn't have a case for that concrete type — it fell through to the base mapping | Add the missing case; consider the reflection-based "every subtype has a case" test from §5.2 |
| Package removal (`dotnet remove package AutoMapper`) fails with "not found" | The package is referenced transitively (via a shared internal package) rather than directly in this csproj | Find the actual referencing project with `dotnet list package --include-transitive`, remove it there instead |
| A `Profile` file still has content after all `CreateMap<>()` calls are removed | Something non-AutoMapper-specific was defined in the same file (helper methods, constants) | Keep that content; move it out of the `Profile`-derived class into a plain class before deleting the AutoMapper base type |

---

<a id="final-checklist"></a>

## 8. Final checklist and change summary

Run through this as the single acceptance pass. It summarizes every GATE from §0 through
§6 — if any line is unchecked, the migration is not done, regardless of whether the
build succeeds (a clean build proves the code compiles, not that every mapping was
migrated).

### Acceptance checklist

- [ ] **Discovery (§1) is complete**: every `CreateMap<>()`, every call site, every
      global option is recorded in your work-list table.
- [ ] **Library is in place (§2)**: correct tier chosen per project; `RECO.Mapping`
      (or its fallback variant) builds with 0 errors.
- [ ] **Every mapping pair has explicit replacement code (§3)**: every destination
      member is assigned somewhere, or is a recorded, deliberate unmapped-by-design
      entry. Every §3.14 flattening candidate was individually checked, not assumed.
- [ ] **Every call site rewritten (§4)**: `grep -rn "AutoMapper\|IMapper\b"
      --include="*.cs" --include="*.csproj" .` returns nothing in the target codebase.
- [ ] **Every special case resolved by conscious decision, not guesswork (§5)**:
      DI-dependent mappings are services, not statics; polymorphic mappings dispatch
      explicitly; reference cycles are broken deliberately.
- [ ] **Every mapping pair has a passing `MappingVerifier` test, both directions where
      applicable (§6)**, with `unmappedByDesign` lists matching your §3.2 records
      exactly.
- [ ] **Solution builds with no new warnings.**
- [ ] **Pre-existing functional/integration tests covering mapped data still pass,
      unmodified** — your genuine behavioral-regression signal, since the verifier
      proves coverage, not value-correctness.
- [ ] **AutoMapper package reference is gone from every project** that had it.

### Change-summary template (for the PR/commit description)

```
Replace AutoMapper with RECO.Mapping

- Migrated N mapping pairs across M projects (list: <Source→Dest, Source2→Dest2, ...>).
- Library tier used: [interface / fallback / mixed — specify per project if mixed].
- K mapping pairs required special handling: [DI-dependent services / inheritance
  dispatch / cycle-breaking / ProjectTo replacement — list which and why].
- Added P new MappingVerifier-based tests (Q before this change, Q+P after).
- Flattened properties found and made explicit: [list, or "none found"].
- AutoMapper package reference removed from: [project list].
```

Fill in the bracketed values from your own discovery table and test run — this summary
is the record a reviewer (human or a future LLM) needs to trust the migration without
re-deriving your discovery work from scratch.

---

This is the end of the AutoMapper removal guide. See the [multi-file README](README.md)
for the same content split across navigable files, or the repository root for the
RecoAPI-specific .NET 6 → .NET 10 upgrade playbook (a separate, independent document).
