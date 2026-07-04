> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 7 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Phase 4 — Rewire pipeline](05-rewire-pipeline.md) · Next: [Troubleshooting](07-troubleshooting.md)

## 6. Phase 5 — Verification tests

### 6.1 Create the test project

**File `tests/Infrastructure/RECO.API.Persistence.Tests/RECO.API.Persistence.Tests.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.7.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\src\Common\RECO.Mapping\RECO.Mapping.csproj" />
    <ProjectReference Include="..\..\..\src\Domain\RECO.API.Domain\RECO.API.Domain.csproj" />
    <ProjectReference Include="..\..\..\src\Infrastructure\RECO.API.Persistence\RECO.API.Persistence.csproj" />
  </ItemGroup>

</Project>
```

Add it: `dotnet sln <your-solution-file>.sln add tests/Infrastructure/RECO.API.Persistence.Tests/RECO.API.Persistence.Tests.csproj`

### 6.2 Coverage tests — 3 per entity

Create `Mapping/MappingCoverageTests.cs` in the test project. Appendix B contains the
complete file for the 8 baseline entities. For each entity, the pattern is exactly three
tests; for **extra** entities, append three more tests following the same pattern:

```csharp
[Test]
public void <E>_MapFrom_DataModel_CoversAllMembers() =>
    MappingVerifier.AssertAllMembersMapped<DataModels.<E>, DbModels.<E>>(
        DbModels.<E>.MapFrom,
        /* the ignore list of the CreateMap<Domain.E, Models.E> map, as strings */);

[Test]
public void <E>_MapTo_DataModel_CoversAllMembers() =>
    MappingVerifier.AssertAllMembersMapped<DbModels.<E>, DataModels.<E>>(
        db => db.MapTo());     // no ignore list: every domain property must be mapped

[Test]
public void <E>_CloneScalars_CoversAllMembers() =>
    MappingVerifier.AssertAllMembersMapped<DbModels.<E>, DbModels.<E>>(
        db => db.CloneScalars(),
        /* the ignore list of the CreateMap<Models.E, Models.E> SELF-map — read it
           separately; it may differ from the MapFrom ignore list */);
```

### 6.3 Verifier self-tests

**File `Mapping/MappingVerifierSelfTests.cs`** (complete, use verbatim):

```csharp
using NUnit.Framework;
using RECO.Mapping.Verification;

namespace RECO.API.Persistence.Tests.Mapping
{
	/// <summary>
	/// Meta-tests proving the verifier itself catches the failure modes it exists for.
	/// </summary>
	[TestFixture]
	public class MappingVerifierSelfTests
	{
		private class Source
		{
			public int Id { get; set; }
			public string Name { get; set; } = "";
			public string Secret { get; set; } = "";
		}

		private class Destination
		{
			public int Id { get; set; }
			public string Name { get; set; } = "";
			public string Secret { get; set; } = "";
		}

		[Test]
		public void Detects_forgotten_property()
		{
			// 'Name' is neither mapped nor declared unmapped-by-design -> must fail.
			Assert.Throws<MappingVerificationException>(() =>
				MappingVerifier.AssertAllMembersMapped<Source, Destination>(
					s => new Destination { Id = s.Id },
					"Secret"));
		}

		[Test]
		public void Detects_accidentally_copied_ignored_property()
		{
			// 'Secret' is declared unmapped-by-design but the mapping copies it -> must fail.
			Assert.Throws<MappingVerificationException>(() =>
				MappingVerifier.AssertAllMembersMapped<Source, Destination>(
					s => new Destination { Id = s.Id, Name = s.Name, Secret = s.Secret },
					"Secret"));
		}

		[Test]
		public void Passes_for_complete_mapping()
		{
			Assert.DoesNotThrow(() =>
				MappingVerifier.AssertAllMembersMapped<Source, Destination>(
					s => new Destination { Id = s.Id, Name = s.Name },
					"Secret"));
		}
	}
}
```

### 6.4 ✅ GATE 5 — final acceptance

```bash
dotnet build <your-solution-file>.sln --no-incremental
dotnet test  <your-solution-file>.sln --no-build
```

Acceptance conditions:

1. Build: **0 errors, 0 warnings**.
2. The new Persistence test project: **all tests pass**. Expected count =
   `3 × (number of entities in [ENTITIES]) + 3` (baseline: 27).
3. Pre-existing test projects: results must be **equal or better** than before the
   upgrade. **Classification rule for failures:** a failing test whose error is
   `SqlException` (server not found / network-related) *and* whose code contains a
   hardcoded connection string is **environmental** — it requires a specific live SQL
   Server and fails identically on the pre-upgrade code from any machine without that
   server. Document such failures; they do not block acceptance. Any other failure is a
   regression — stop and fix.
4. No AutoMapper remnants — this must output nothing (comments in your own new files
   excepted):
   ```bash
   grep -rn "AutoMapper\|IMapper" --include="*.cs" --include="*.csproj" src tests | grep -v "RECO.Mapping\|IMappedFrom\|obj/\|bin/"
   ```

### 6.5 Optional runtime smoke test (machine without the database)

The service cannot fully start without its SQL Server (the hosted service starts a
queue that reads the DB — identical behavior before and after the upgrade). To validate
everything **up to** the database boundary:

1. In the Service project's `appsettings.json`, temporarily set the Serilog `MSSqlServer`
   sink argument `"autoCreateSqlTable"` to `false` (this defers the log-DB connection).
2. Run: `ASPNETCORE_ENVIRONMENT=Development dotnet run` in the Service project folder.
3. **PASS** = startup proceeds through Serilog configuration and the full DI container
   build (this proves every new generic constraint resolves), then fails with a
   `SqlException` in the hosted-service/queue startup. **FAIL** = any error *before*
   that point (a DI resolution error, a Serilog configuration error, a missing type) —
   that is a real regression; consult §7.
4. Revert `"autoCreateSqlTable"` to `true`.

### 6.6 Deployment follow-ups (record these for the operators)

- All **deployed plugin module DLLs** (filters/subscribers loaded from the module
  folders at runtime) were built for .NET 6 and must be **rebuilt against net10.0**.
  This applies to every module, including ones that exist only in your copy.
- The Development configuration file is git-ignored but loaded as *required* at startup;
  fresh checkouts must create it before running.
- The production SQL Servers must accept the amended connection strings (or replace
  `TrustServerCertificate=True` with proper certificate trust).

---

