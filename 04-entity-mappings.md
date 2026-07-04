> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 5 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Phase 2 — Mapping library](03-mapping-library.md) · Next: [Phase 4 — Rewire pipeline](05-rewire-pipeline.md)

## 4. Phase 3 — Write one mapping file per entity

For **every** entity in your `[ENTITIES]` list, create
`src/Infrastructure/RECO.API.Persistence/Mapping/<Entity>Mapping.cs` (same folder as the
old `MapperFactory.cs`).

### 4.1 If your entity matches the baseline — use the verbatim files in Appendix A

Appendix A contains complete, ready-to-use files for the 8 baseline entities. **Before
using one, check its precondition**: open your domain record and your DB model for that
entity and confirm every property named in the file exists with the same type. If yes,
copy the file unchanged. If not — or if your `[ENTITIES]` list has entities beyond the
baseline 8 — produce the file with the algorithm in §4.2.

### 4.2 Derivation algorithm (for extra or diverging entities)

The old `MapperFactory` is the **specification**. For entity `E` (domain record
`Domain.DataModels.E`, DB model `Persistence.Models.E`) it contains up to three
`cfg.CreateMap<...>` calls. Translate them as follows.

**Template** (fill the three method bodies by the rules below):

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class E : IDualMapped<E, DataModels.E>
	{
		public static E MapFrom(DataModels.E source) => new()
		{
			/* RULE A assignments */
		};

		public DataModels.E MapTo() => new()
		{
			/* RULE B assignments */
		};

		public E CloneScalars()
		{
			var clone = new E();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(E target)
		{
			/* RULE C assignments */
		}
	}
}
```

**RULE A — `MapFrom` (from `CreateMap<Domain.E, Models.E>`):** the destination is the DB
model. For every public settable property of the DB model that is **not** in that map's
`ForMember(x => x.P, opt => opt.Ignore())` list, add `P = source.<matching>`, where
`<matching>` is the domain property whose name equals `P` **case-insensitively** (e.g.
DB `TblCodeId` ← domain `TblCodeID`; DB `ObjectConceptUri` ← domain `ObjectConceptURI`).
Type bridge: domain `bool` → DB `bool?` assigns directly. Do not add assignments for
ignored properties — the ignore IS the specification.

**RULE B — `MapTo` (from `CreateMap<Models.E, Domain.E>`):** the destination is the
domain record. This map has **no ignores** in the baseline profile, so assign **every**
public property of the domain record (they are `init`-settable; the object-initializer
syntax in the template handles that) from the case-insensitive matching DB property.
Type bridge: DB `bool?` → domain `bool` must use `.GetValueOrDefault()` (this replicates
AutoMapper's null→default behavior). DB `bool?` → domain `bool?` assigns directly.
`Created` and `Modified` ARE assigned here (domain records carry them).

**RULE C — `CopyScalarsTo` (from `CreateMap<Models.E, Models.E>`):** destination and
source are both the DB model. For every public settable property **not** in that map's
ignore list, add `target.P = P;`. ⚠️ The self-map's ignore list can **differ** from the
`MapFrom` ignore list — in the baseline, the `TblCode` self-map copies
`Relation_ParentCodeTypes` and `Relation_ParentCodes` (they are absent from its ignore
list) while the domain→DB map ignores them; same for `Relation_ParentCodeTypes` on
`TblCodeType`. Read each map's own ignore list — never assume they are identical.

**Universal constants of the baseline profile** (apply unless your MapperFactory says
otherwise): `User`, `Created`, `Modified` are ignored on every DB-model destination
(RULE A and RULE C); navigation properties (virtual EF references and collections) are
ignored on every DB-model destination.

### ✅ GATE 3

Every entity in `[ENTITIES]` has a `<Entity>Mapping.cs` file. Each file compiles in
isolation is not checkable yet (Persistence still references AutoMapper types) — the
compile check happens at GATE 4.

---

