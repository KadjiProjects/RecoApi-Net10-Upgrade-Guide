> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 10 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Appendix A — verbatim mapping files](appendix-a-entity-mappings.md) · 

## Appendix B — verbatim coverage test file for the 8 baseline entities

**File `tests/Infrastructure/RECO.API.Persistence.Tests/Mapping/MappingCoverageTests.cs`.**
For entities beyond the baseline 8, append three tests per entity using the §6.2 pattern
with ignore lists read from your own MapperFactory.

```csharp
using NUnit.Framework;
using RECO.Mapping.Verification;
using DataModels = RECO.API.Domain.DataModels;
using DbModels = RECO.API.Persistence.Models;

namespace RECO.API.Persistence.Tests.Mapping
{
	/// <summary>
	/// Coverage verification for the hand-written mappings — the replacement for AutoMapper's
	/// <c>AssertConfigurationIsValid()</c>. If a property is added to a model and not mapped
	/// (or not declared unmapped-by-design below), the corresponding test fails.
	/// The unmapped-by-design lists mirror the ForMember(...Ignore()) configuration of the
	/// original MapperFactory profile exactly.
	/// </summary>
	[TestFixture]
	public class MappingCoverageTests
	{
		// ---------- TblCode ----------

		[Test]
		public void TblCode_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblCode, DbModels.TblCode>(
				DbModels.TblCode.MapFrom,
				"User", "Created", "Modified",
				"TblCodeType", "TblCodeExternalRels", "TblCodeRelTblCodeManies", "TblCodeRelTblCodeOnes",
				"TblData", "TblSynonyms",
				"Relation_TblCodeType", "Relation_ParentCodeTypes", "Relation_ParentCodes");

		[Test]
		public void TblCode_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCode, DataModels.TblCode>(
				db => db.MapTo());

		[Test]
		public void TblCode_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCode, DbModels.TblCode>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"TblCodeType", "TblCodeExternalRels", "TblCodeRelTblCodeManies", "TblCodeRelTblCodeOnes",
				"TblData", "TblSynonyms",
				"Relation_TblCodeType"); // Relation_ParentCodeTypes / Relation_ParentCodes ARE copied, per original profile

		// ---------- TblCodeExternalRel ----------

		[Test]
		public void TblCodeExternalRel_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblCodeExternalRel, DbModels.TblCodeExternalRel>(
				DbModels.TblCodeExternalRel.MapFrom,
				"User", "Created", "Modified",
				"RelType", "TblCode",
				"Relation_TblCode", "Relation_TblCodeType");

		[Test]
		public void TblCodeExternalRel_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeExternalRel, DataModels.TblCodeExternalRel>(
				db => db.MapTo());

		[Test]
		public void TblCodeExternalRel_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeExternalRel, DbModels.TblCodeExternalRel>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"RelType", "TblCode",
				"Relation_TblCode", "Relation_TblCodeType");

		// ---------- TblCodeRel ----------

		[Test]
		public void TblCodeRel_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblCodeRel, DbModels.TblCodeRel>(
				DbModels.TblCodeRel.MapFrom,
				"User", "Created", "Modified",
				"TblCodeMany", "TblCodeOne", "TblCodeTypeRel",
				"Relation_TblCodeOne", "Relation_TblCodeMany",
				"Relation_TblCodeTypeOne", "Relation_TblCodeTypeMany", "Relation_TblCodeTypeRel");

		[Test]
		public void TblCodeRel_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeRel, DataModels.TblCodeRel>(
				db => db.MapTo());

		[Test]
		public void TblCodeRel_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeRel, DbModels.TblCodeRel>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"TblCodeMany", "TblCodeOne", "TblCodeTypeRel",
				"Relation_TblCodeOne", "Relation_TblCodeMany",
				"Relation_TblCodeTypeOne", "Relation_TblCodeTypeMany", "Relation_TblCodeTypeRel");

		// ---------- TblCodeType ----------

		[Test]
		public void TblCodeType_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblCodeType, DbModels.TblCodeType>(
				DbModels.TblCodeType.MapFrom,
				"User", "Created", "Modified",
				"TblCodeTypeRelTblCodeTypeChds", "TblCodeTypeRelTblCodeTypePars", "TblCodes",
				"Relation_ParentCodeTypes");

		[Test]
		public void TblCodeType_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeType, DataModels.TblCodeType>(
				db => db.MapTo());

		[Test]
		public void TblCodeType_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeType, DbModels.TblCodeType>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"TblCodeTypeRelTblCodeTypeChds", "TblCodeTypeRelTblCodeTypePars", "TblCodes");
				// Relation_ParentCodeTypes IS copied, per original profile

		// ---------- TblCodeTypeRel ----------

		[Test]
		public void TblCodeTypeRel_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblCodeTypeRel, DbModels.TblCodeTypeRel>(
				DbModels.TblCodeTypeRel.MapFrom,
				"User", "Created", "Modified",
				"RelType", "TblCodeTypeChd", "TblCodeTypePar", "TblCodeRels",
				"Relation_TblCodeTypeParent", "Relation_TblCodeTypeChild", "Relation_RelType");

		[Test]
		public void TblCodeTypeRel_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeTypeRel, DataModels.TblCodeTypeRel>(
				db => db.MapTo());

		[Test]
		public void TblCodeTypeRel_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblCodeTypeRel, DbModels.TblCodeTypeRel>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"RelType", "TblCodeTypeChd", "TblCodeTypePar", "TblCodeRels",
				"Relation_TblCodeTypeParent", "Relation_TblCodeTypeChild", "Relation_RelType");

		// ---------- TblData ----------

		[Test]
		public void TblData_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblData, DbModels.TblData>(
				DbModels.TblData.MapFrom,
				"User", "Created", "Modified",
				"TblCode", "TblDataType",
				"Relation_TblCode", "Relation_TblCodeType", "Relation_TblDataType");

		[Test]
		public void TblData_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblData, DataModels.TblData>(
				db => db.MapTo());

		[Test]
		public void TblData_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblData, DbModels.TblData>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"TblCode", "TblDataType",
				"Relation_TblCode", "Relation_TblCodeType", "Relation_TblDataType");

		// ---------- TblDataType ----------

		[Test]
		public void TblDataType_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblDataType, DbModels.TblDataType>(
				DbModels.TblDataType.MapFrom,
				"User", "Created", "Modified", "TblData");

		[Test]
		public void TblDataType_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblDataType, DataModels.TblDataType>(
				db => db.MapTo());

		[Test]
		public void TblDataType_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblDataType, DbModels.TblDataType>(
				db => db.CloneScalars(),
				"User", "Created", "Modified", "TblData");

		// ---------- TblSynonym ----------

		[Test]
		public void TblSynonym_MapFrom_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DataModels.TblSynonym, DbModels.TblSynonym>(
				DbModels.TblSynonym.MapFrom,
				"User", "Created", "Modified",
				"TblCode",
				"Relation_TblCode", "Relation_TblCodeType");

		[Test]
		public void TblSynonym_MapTo_DataModel_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblSynonym, DataModels.TblSynonym>(
				db => db.MapTo());

		[Test]
		public void TblSynonym_CloneScalars_CoversAllMembers() =>
			MappingVerifier.AssertAllMembersMapped<DbModels.TblSynonym, DbModels.TblSynonym>(
				db => db.CloneScalars(),
				"User", "Created", "Modified",
				"TblCode",
				"Relation_TblCode", "Relation_TblCodeType");
	}
}
```
