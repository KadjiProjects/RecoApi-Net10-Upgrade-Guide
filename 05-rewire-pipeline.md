> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 6 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Phase 3 — Entity mappings](04-entity-mappings.md) · Next: [Phase 5 — Verification](06-verification.md)

## 5. Phase 4 — Rewire the pipeline and delete AutoMapper code

### 5.1 `Repository.cs` (Persistence)

Three edits:

```diff
-using AutoMapper;
+using RECO.Mapping;
```

```diff
     internal class Repository<TDbModel> : IRepository<TDbModel>
-        where TDbModel : class, ITrackingData, IDataRecordWithRelationships, IDbModel<TDbModel>, new()
+        where TDbModel : class, ITrackingData, IDataRecordWithRelationships, IDbModel<TDbModel>, IScalarCopyable<TDbModel>, new()
     {
         private readonly Func<IContextAdapter<TDbModel>> _contextAdapterFactory;
-        private readonly IMapper _mapper;
         private readonly ILogger<Repository<TDbModel>> _logger;
         private readonly IRelatedRecordsLoader<TDbModel> _relatedRecordsLoader;

-        internal Repository(Func<IContextAdapter<TDbModel>> contextAdapterFactory, IMapper mapper, ILogger<Repository<TDbModel>> logger,
+        internal Repository(Func<IContextAdapter<TDbModel>> contextAdapterFactory, ILogger<Repository<TDbModel>> logger,
             IRelatedRecordsLoader<TDbModel> relatedRecordsLoader = null)
         {
             _contextAdapterFactory = contextAdapterFactory;
-            _mapper = mapper;
             _logger = logger;
             _relatedRecordsLoader = relatedRecordsLoader;
         }
```

In the update method (the one that loads `currentRecord` from the DbSet):

```diff
-            var oldContent = _mapper.Map<TDbModel>(currentRecord);
-
-            // update the current record with new data
-            _mapper.Map(dbModel, currentRecord);
+            var oldContent = currentRecord.CloneScalars();
+
+            // update the current record with new data (scalars only — navigations stay untouched)
+            dbModel.CopyScalarsTo(currentRecord);
```

### 5.2 `RepositoryFactory.cs` (Persistence)

```diff
-using RECO.API.Persistence.Mapping;
 using RECO.API.Persistence.Models;
 using RECO.API.Persistence.RelatedRecordsLoading;
+using RECO.Mapping;
```

Add `IScalarCopyable<TDbModel>` to the generic constraints of **both**
`CreateRepository<TDbModel>` and `MakeRepository<TDbModel, TDataModel>` (insert after
`IDbModel<TDbModel>,`), and remove the mapper argument:

```diff
             return new Repository<TDbModel>(
                             contextAdapterFactory,
-                            MapperFactory.CreateMapper(),
                             _loggerFactory.CreateLogger<Repository<TDbModel>>(),
                             _relatedRecordsLoadersFactory.CreateLoader<TDbModel>());
```

### 5.3 `DependencyInjection.cs` (Persistence)

Remove the line `using RECO.API.Persistence.Mapping;` and the line:

```csharp
services.AddSingleton(serviceProvider => MapperFactory.CreateMapper());
```

### 5.4 `ActionProcessor.cs` (Processing)

```diff
-using AutoMapper;
 using Microsoft.Extensions.Logging;
```

```diff
 using RECO.API.Domain.Action;
 using RECO.API.Domain.Models;
+using RECO.Mapping;
```

```diff
     public class ActionProcessor<TDataModel, TDbModel> : IActionProcessor<TDataModel>
         where TDataModel : IDataRecord
-        where TDbModel : IDataRecordWithRelationships
+        where TDbModel : IDataRecordWithRelationships, IDualMapped<TDbModel, TDataModel>
```

Delete the `private readonly IMapper _mapper;` field, remove `IMapper mapper` from the
constructor parameters, and delete `_mapper = mapper;` from the constructor body.

Replace all mapper call sites (find them with `grep -n "_mapper" <file>`; the baseline
has exactly four):

```diff
-ModifiedRecord = saveResult.Success ? _mapper.Map<TDataModel>(saveResult.NewContent) : default,
+ModifiedRecord = saveResult.Success ? saveResult.NewContent.MapTo() : default,
```
```diff
-ModifiedRecord = sr.Success ? _mapper.Map<TDataModel>(sr.NewContent) : default
+ModifiedRecord = sr.Success ? sr.NewContent.MapTo() : default
```
```diff
-TDbModel data = _mapper.Map<TDbModel>(request.Parameter);
+TDbModel data = TDbModel.MapFrom(request.Parameter);
```
```diff
-var data = _mapper.Map<List<TDbModel>>(request.Parameters);
+var data = request.Parameters.MapFromAll<TDbModel, TDataModel>();
```

If your copy has additional `_mapper.Map` call sites, convert each by the same pattern:
`Map<TDataModel>(x)` → `x.MapTo()`; `Map<TDbModel>(y)` → `TDbModel.MapFrom(y)`;
`Map<List<TDbModel>>(ys)` → `ys.MapFromAll<TDbModel, TDataModel>()`.

The Processing project's `DependencyInjection.cs` needs **no change** — DI resolves the
smaller constructor automatically.

### 5.5 Delete the AutoMapper factory files

```bash
rm src/Infrastructure/RECO.API.Persistence/Mapping/MapperFactory.cs
rm src/Infrastructure/RECO.API.Persistence/Mapping/IMapperFactory.cs
```

### ✅ GATE 4

```bash
dotnet build <your-solution-file>.sln --no-incremental
```

Must report **Build succeeded, 0 Error(s), 0 Warning(s)**. On failure, consult §7 before
changing anything else.

---

