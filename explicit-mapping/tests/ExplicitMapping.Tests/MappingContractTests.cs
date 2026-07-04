using NUnit.Framework;

namespace ExplicitMapping.Tests;

/// <summary>
/// Exercises every contract in the library (<see cref="IMappedFrom{TSelf,TSource}"/>,
/// <see cref="IMapsTo{TDestination}"/>, <see cref="IScalarCopyable{TSelf}"/>,
/// <see cref="IDualMapped{TSelf,TOther}"/>, and the <see cref="MappingExtensions"/> list
/// helpers) against the demo <see cref="PersonEntity"/> / <see cref="PersonDto"/> pair.
/// This is the shape the migration guide's per-entity mapping partials follow — see the
/// guide's §3 (migration patterns) and §6 (verification).
/// </summary>
[TestFixture]
public class MappingContractTests
{
    [Test]
    public void MapFrom_populates_every_scalar_from_the_source()
    {
        var dto = new PersonDto { Id = 7, FirstName = "Ada", LastName = "Lovelace", CreatedUtc = new DateTime(1815, 12, 10) };

        var entity = PersonEntity.MapFrom(dto);

        Assert.Multiple(() =>
        {
            Assert.That(entity.Id, Is.EqualTo(7));
            Assert.That(entity.FirstName, Is.EqualTo("Ada"));
            Assert.That(entity.LastName, Is.EqualTo("Lovelace"));
            Assert.That(entity.CreatedUtc, Is.EqualTo(new DateTime(1815, 12, 10)));
            Assert.That(entity.Tags, Is.Empty, "navigation-style properties are not sourced from the DTO");
        });
    }

    [Test]
    public void MapTo_projects_the_entity_back_to_the_dto_shape()
    {
        var entity = new PersonEntity { Id = 3, FirstName = "Grace", LastName = "Hopper", CreatedUtc = new DateTime(1906, 12, 9) };

        var dto = entity.MapTo();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(3));
            Assert.That(dto.FirstName, Is.EqualTo("Grace"));
            Assert.That(dto.LastName, Is.EqualTo("Hopper"));
            Assert.That(dto.CreatedUtc, Is.EqualTo(new DateTime(1906, 12, 9)));
        });
    }

    [Test]
    public void MapFromAll_and_MapToAll_map_whole_lists_with_zero_reflection()
    {
        PersonDto[] dtos =
        [
            new() { Id = 1, FirstName = "Alan" },
            new() { Id = 2, FirstName = "Barbara" },
        ];

        var entities = dtos.MapFromAll<PersonEntity, PersonDto>();
        Assert.That(entities.Select(e => e.Id), Is.EqualTo(new[] { 1, 2 }));

        var roundTripped = entities.MapToAll<PersonEntity, PersonDto>();
        Assert.That(roundTripped.Select(d => d.FirstName), Is.EqualTo(new[] { "Alan", "Barbara" }));
    }

    [Test]
    public void CopyScalarsTo_updates_an_already_tracked_instance_without_touching_navigation_properties()
    {
        // Simulates Repository<T>.Update: `currentRecord` is the EF-tracked instance,
        // `incoming` is the freshly mapped request payload.
        var currentRecord = new PersonEntity { Id = 1, FirstName = "Old", Tags = ["kept-relationship-row"] };
        var incoming = new PersonEntity { Id = 1, FirstName = "New", LastName = "Name" };

        incoming.CopyScalarsTo(currentRecord);

        Assert.Multiple(() =>
        {
            Assert.That(currentRecord.FirstName, Is.EqualTo("New"));
            Assert.That(currentRecord.LastName, Is.EqualTo("Name"));
            Assert.That(currentRecord.Tags, Is.EqualTo(new[] { "kept-relationship-row" }),
                "CopyScalarsTo must never touch navigation/relationship properties");
        });
    }

    [Test]
    public void CloneScalars_produces_an_independent_snapshot_excluding_navigation_properties()
    {
        // Simulates Repository<T>.Update: `oldContent = currentRecord.CloneScalars()` taken
        // BEFORE the update, so the caller can report what changed.
        var currentRecord = new PersonEntity { Id = 5, FirstName = "Before" };
        currentRecord.Tags.Add("relationship-row");

        var snapshot = currentRecord.CloneScalars();
        currentRecord.FirstName = "After"; // mutate the original after snapshotting

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.FirstName, Is.EqualTo("Before"), "the snapshot must not alias the live instance");
            Assert.That(snapshot.Tags, Is.Empty, "the snapshot excludes navigation properties by design");
        });
    }
}
