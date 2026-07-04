namespace RECO.Mapping.Tests;

/// <summary>
/// A minimal "DTO" side of a mapping pair — stands in for whatever your domain/API model
/// looks like (e.g. RecoAPI's <c>Domain.Models.TblCode</c>).
/// </summary>
public sealed class PersonDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// A minimal "entity" side of a mapping pair — stands in for whatever your EF Core /
/// persistence model looks like (e.g. RecoAPI's <c>Persistence.Models.TblCode</c>).
/// Implements <see cref="IDualMapped{TSelf, TOther}"/> in full: bidirectional mapping plus
/// scalar clone/copy for the repository update path AutoMapper used to power via
/// <c>_mapper.Map(dbModel, currentRecord)</c>.
/// </summary>
public sealed class PersonEntity : IDualMapped<PersonEntity, PersonDto>
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Stands in for an EF Core navigation/relationship property. Deliberately excluded from
    /// every mapping direction below — this is the property class that AutoMapper's
    /// <c>ForMember(x => x.Tags, opt => opt.Ignore())</c> used to protect, and that
    /// <see cref="Verification.MappingVerifier"/> below proves stays protected.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    public static PersonEntity MapFrom(PersonDto source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        LastName = source.LastName,
        CreatedUtc = source.CreatedUtc,
        // Tags is populated by the repository from a separate relationship table, not the DTO.
    };

    public PersonDto MapTo() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        CreatedUtc = CreatedUtc,
    };

    public PersonEntity CloneScalars() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        CreatedUtc = CreatedUtc,
        // Tags excluded: CloneScalars() is used to snapshot "before" state for change tracking,
        // and must never fork the navigation collection.
    };

    public void CopyScalarsTo(PersonEntity target)
    {
        target.Id = Id;
        target.FirstName = FirstName;
        target.LastName = LastName;
        target.CreatedUtc = CreatedUtc;
        // target.Tags is left completely untouched — this is the line that replaces
        // AutoMapper's in-place Map(dbModel, currentRecord) in Repository.Update.
    }
}
