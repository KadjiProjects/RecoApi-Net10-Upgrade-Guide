> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 2 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Ground rules](00-ground-rules.md) · Next: [Phase 1 — Retarget & packages](02-retarget-and-packages.md)

## 1. Phase 0 — Discovery (produces your work lists)

Run these from the solution root (the folder containing the `.sln` file). Record each
output — later phases refer to them as **[PROJECTS]**, **[AUTOMAPPER-PROJECTS]**,
**[ENTITIES]**, **[CONNSTRINGS]**.

```bash
# [PROJECTS] — every project file. ALL of them get retargeted.
find src tests -name "*.csproj"

# [AUTOMAPPER-PROJECTS] — projects whose csproj references AutoMapper.
grep -rl "AutoMapper" --include="*.csproj" src tests

# Which projects USE AutoMapper in code (expect: Persistence + Processing only;
# any csproj reference not in this list is dead and simply gets removed):
grep -rl "using AutoMapper\|IMapper" --include="*.cs" src tests

# [ENTITIES] — the mapping surface. Open the MapperFactory file and list every DISTINCT
# entity name appearing in cfg.CreateMap<...>() calls:
grep -rn "CreateMap<" src --include="*.cs"

# [CONNSTRINGS] — every SQL connection string in the solution (config AND code):
grep -rniE "data source=|server=" --include="*.json" --include="*.cs" --include="*.config" src tests | grep -viE "bin/|obj/"

# The solution file name (used by later commands; baseline: RECO.API.sln):
ls *.sln
```

Baseline expectations (yours may be larger — that is fine): 11 projects; 3 AutoMapper
csproj references of which 2 have code usage; 8 entities (`TblCode`,
`TblCodeExternalRel`, `TblCodeRel`, `TblCodeType`, `TblCodeTypeRel`, `TblData`,
`TblDataType`, `TblSynonym`); 4 connection strings.

### ✅ GATE 0

You have written down all four lists. `[ENTITIES]` is non-empty and every entity in it
has (a) a domain record in the Domain project's `DataModels` folder and (b) a DB model
class in the Persistence project's `Models` folder. If any entity lacks one of the two,
stop and report.

---

