> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 9 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Troubleshooting](07-troubleshooting.md) · Next: [Appendix B — verbatim coverage tests](appendix-b-coverage-tests.md)

## Appendix A — verbatim mapping files for the 8 baseline entities

> **Precondition for each file:** your domain record and DB model contain exactly the
> properties the file references. Verify by opening both model files. On any mismatch,
> derive the file with §4.2 instead of editing blindly.

### A.1 `TblCodeMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	// Explicit, compile-time-checked mapping. Replaces the AutoMapper profile in MapperFactory:
	// properties that were ForMember(...Ignore()) are simply not assigned.
	public partial class TblCode : IDualMapped<TblCode, DataModels.TblCode>
	{
		public static TblCode MapFrom(DataModels.TblCode source) => new()
		{
			TblCodeId = source.TblCodeID,
			TblCodeTypeId = source.TblCodeTypeID,
			Code = source.Code,
			Description = source.Description,
			Visibility = source.Visibility,
			Deprecated = source.Deprecated,
			LongDescription = source.LongDescription,
			// unmapped by design: User, Created, Modified, navigation and Relation_* properties
		};

		public DataModels.TblCode MapTo() => new()
		{
			TblCodeID = TblCodeId,
			TblCodeTypeID = TblCodeTypeId,
			Code = Code,
			Description = Description,
			Visibility = Visibility.GetValueOrDefault(),
			Deprecated = Deprecated.GetValueOrDefault(),
			LongDescription = LongDescription,
			Created = Created,
			Modified = Modified,
		};

		public TblCode CloneScalars()
		{
			var clone = new TblCode();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblCode target)
		{
			target.TblCodeId = TblCodeId;
			target.TblCodeTypeId = TblCodeTypeId;
			target.Code = Code;
			target.Description = Description;
			target.Visibility = Visibility;
			target.Deprecated = Deprecated;
			target.LongDescription = LongDescription;
			// Per the original profile, these two Relation_* properties are copied
			// (only Relation_TblCodeType was ignored):
			target.Relation_ParentCodeTypes = Relation_ParentCodeTypes;
			target.Relation_ParentCodes = Relation_ParentCodes;
			// unmapped by design: User, Created, Modified, navigations, Relation_TblCodeType
		}
	}
}
```

### A.2 `TblCodeExternalRelMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblCodeExternalRel : IDualMapped<TblCodeExternalRel, DataModels.TblCodeExternalRel>
	{
		public static TblCodeExternalRel MapFrom(DataModels.TblCodeExternalRel source) => new()
		{
			TblCodeExternalRelId = source.TblCodeExternalRelID,
			TblCodeId = source.TblCodeID,
			ObjectConceptUri = source.ObjectConceptURI,
			RelTypeId = source.RelTypeID,
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		};

		public DataModels.TblCodeExternalRel MapTo() => new()
		{
			TblCodeExternalRelID = TblCodeExternalRelId,
			TblCodeID = TblCodeId,
			ObjectConceptURI = ObjectConceptUri,
			RelTypeID = RelTypeId,
			Created = Created,
			Modified = Modified,
		};

		public TblCodeExternalRel CloneScalars()
		{
			var clone = new TblCodeExternalRel();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblCodeExternalRel target)
		{
			target.TblCodeExternalRelId = TblCodeExternalRelId;
			target.TblCodeId = TblCodeId;
			target.ObjectConceptUri = ObjectConceptUri;
			target.RelTypeId = RelTypeId;
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		}
	}
}
```

### A.3 `TblCodeRelMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblCodeRel : IDualMapped<TblCodeRel, DataModels.TblCodeRel>
	{
		public static TblCodeRel MapFrom(DataModels.TblCodeRel source) => new()
		{
			TblCodeOneId = source.TblCodeOneID,
			TblCodeManyId = source.TblCodeManyID,
			TblCodeTypeRelId = source.TblCodeTypeRelID,
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		};

		public DataModels.TblCodeRel MapTo() => new()
		{
			TblCodeOneID = TblCodeOneId,
			TblCodeManyID = TblCodeManyId,
			TblCodeTypeRelID = TblCodeTypeRelId,
			Created = Created,
			Modified = Modified,
		};

		public TblCodeRel CloneScalars()
		{
			var clone = new TblCodeRel();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblCodeRel target)
		{
			target.TblCodeOneId = TblCodeOneId;
			target.TblCodeManyId = TblCodeManyId;
			target.TblCodeTypeRelId = TblCodeTypeRelId;
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		}
	}
}
```

### A.4 `TblCodeTypeMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblCodeType : IDualMapped<TblCodeType, DataModels.TblCodeType>
	{
		public static TblCodeType MapFrom(DataModels.TblCodeType source) => new()
		{
			TblCodeTypeId = source.TblCodeTypeID,
			CodeType = source.CodeType,
			Description = source.Description,
			Definition = source.Definition,
			VerX = source.VerX,
			VerY = source.VerY,
			VerDate = source.VerDate,
			Visibility = source.Visibility,
			// unmapped by design: User, Created, Modified, navigations, Relation_ParentCodeTypes
		};

		public DataModels.TblCodeType MapTo() => new()
		{
			TblCodeTypeID = TblCodeTypeId,
			CodeType = CodeType,
			Description = Description,
			Definition = Definition,
			VerX = VerX,
			VerY = VerY,
			VerDate = VerDate,
			Visibility = Visibility,
			Created = Created,
			Modified = Modified,
		};

		public TblCodeType CloneScalars()
		{
			var clone = new TblCodeType();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblCodeType target)
		{
			target.TblCodeTypeId = TblCodeTypeId;
			target.CodeType = CodeType;
			target.Description = Description;
			target.Definition = Definition;
			target.VerX = VerX;
			target.VerY = VerY;
			target.VerDate = VerDate;
			target.Visibility = Visibility;
			// Per the original profile, Relation_ParentCodeTypes IS copied in the clone map:
			target.Relation_ParentCodeTypes = Relation_ParentCodeTypes;
			// unmapped by design: User, Created, Modified, navigations
		}
	}
}
```

### A.5 `TblCodeTypeRelMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblCodeTypeRel : IDualMapped<TblCodeTypeRel, DataModels.TblCodeTypeRel>
	{
		public static TblCodeTypeRel MapFrom(DataModels.TblCodeTypeRel source) => new()
		{
			TblCodeTypeRelId = source.TblCodeTypeRelID,
			TblCodeTypeParId = source.TblCodeTypeParID,
			TblCodeTypeChdId = source.TblCodeTypeChdID,
			Join = source.Join,
			RelTypeId = source.RelTypeID,
			Description = source.Description,
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		};

		public DataModels.TblCodeTypeRel MapTo() => new()
		{
			TblCodeTypeRelID = TblCodeTypeRelId,
			TblCodeTypeParID = TblCodeTypeParId,
			TblCodeTypeChdID = TblCodeTypeChdId,
			Join = Join,
			RelTypeID = RelTypeId,
			Description = Description,
			Created = Created,
			Modified = Modified,
		};

		public TblCodeTypeRel CloneScalars()
		{
			var clone = new TblCodeTypeRel();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblCodeTypeRel target)
		{
			target.TblCodeTypeRelId = TblCodeTypeRelId;
			target.TblCodeTypeParId = TblCodeTypeParId;
			target.TblCodeTypeChdId = TblCodeTypeChdId;
			target.Join = Join;
			target.RelTypeId = RelTypeId;
			target.Description = Description;
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		}
	}
}
```

### A.6 `TblDataMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblData : IDualMapped<TblData, DataModels.TblData>
	{
		public static TblData MapFrom(DataModels.TblData source) => new()
		{
			TblDataId = source.TblDataID,
			TblCodeId = source.TblCodeID,
			TblDataTypeId = source.TblDataTypeID,
			Value = source.Value,
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		};

		public DataModels.TblData MapTo() => new()
		{
			TblDataID = TblDataId,
			TblCodeID = TblCodeId,
			TblDataTypeID = TblDataTypeId,
			Value = Value,
			Created = Created,
			Modified = Modified,
		};

		public TblData CloneScalars()
		{
			var clone = new TblData();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblData target)
		{
			target.TblDataId = TblDataId;
			target.TblCodeId = TblCodeId;
			target.TblDataTypeId = TblDataTypeId;
			target.Value = Value;
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		}
	}
}
```

### A.7 `TblDataTypeMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblDataType : IDualMapped<TblDataType, DataModels.TblDataType>
	{
		public static TblDataType MapFrom(DataModels.TblDataType source) => new()
		{
			TblDataTypeId = source.TblDataTypeID,
			Description = source.Description,
			// unmapped by design: User, Created, Modified, navigations
		};

		public DataModels.TblDataType MapTo() => new()
		{
			TblDataTypeID = TblDataTypeId,
			Description = Description,
			Created = Created,
			Modified = Modified,
		};

		public TblDataType CloneScalars()
		{
			var clone = new TblDataType();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblDataType target)
		{
			target.TblDataTypeId = TblDataTypeId;
			target.Description = Description;
			// unmapped by design: User, Created, Modified, navigations
		}
	}
}
```

### A.8 `TblSynonymMapping.cs`

```csharp
using RECO.Mapping;
using DataModels = RECO.API.Domain.DataModels;

#nullable disable

namespace RECO.API.Persistence.Models
{
	public partial class TblSynonym : IDualMapped<TblSynonym, DataModels.TblSynonym>
	{
		public static TblSynonym MapFrom(DataModels.TblSynonym source) => new()
		{
			TblSynonymId = source.TblSynonymID,
			TblCodeId = source.TblCodeID,
			Description = source.Description,
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		};

		public DataModels.TblSynonym MapTo() => new()
		{
			TblSynonymID = TblSynonymId,
			TblCodeID = TblCodeId,
			Description = Description,
			Created = Created,
			Modified = Modified,
		};

		public TblSynonym CloneScalars()
		{
			var clone = new TblSynonym();
			CopyScalarsTo(clone);
			return clone;
		}

		public void CopyScalarsTo(TblSynonym target)
		{
			target.TblSynonymId = TblSynonymId;
			target.TblCodeId = TblCodeId;
			target.Description = Description;
			// unmapped by design: User, Created, Modified, navigations, Relation_* properties
		}
	}
}
```

---

