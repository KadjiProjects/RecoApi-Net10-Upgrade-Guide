> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 4 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Phase 1 — Retarget & packages](02-retarget-and-packages.md) · Next: [Phase 3 — Entity mappings](04-entity-mappings.md)

## 3. Phase 2 — Create the mapping class library (complete source)

Create the folder `src/Common/RECO.Mapping/` and the seven files below **exactly as
given**. Then wire it up:

```bash
dotnet sln <your-solution-file>.sln add src/Common/RECO.Mapping/RECO.Mapping.csproj
dotnet add <Persistence csproj> reference src/Common/RECO.Mapping/RECO.Mapping.csproj
dotnet add <Processing  csproj> reference src/Common/RECO.Mapping/RECO.Mapping.csproj
```

**File `src/Common/RECO.Mapping/RECO.Mapping.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <Description>Dependency-free, compile-time-safe object mapping contracts and verification tooling. Replaces AutoMapper.</Description>
  </PropertyGroup>

</Project>
```

**File `src/Common/RECO.Mapping/IMappedFrom.cs`:**

```csharp
namespace RECO.Mapping;

/// <summary>
/// Contract for a type that can construct itself from a <typeparamref name="TSource"/>.
/// The static abstract member makes the factory callable from generic code
/// (<c>TSelf.MapFrom(source)</c>) with full compile-time checking and zero reflection.
/// </summary>
public interface IMappedFrom<TSelf, in TSource> where TSelf : IMappedFrom<TSelf, TSource>
{
    /// <summary>Creates a new <typeparamref name="TSelf"/> populated from <paramref name="source"/>.</summary>
    static abstract TSelf MapFrom(TSource source);
}
```

**File `src/Common/RECO.Mapping/IMapsTo.cs`:**

```csharp
namespace RECO.Mapping;

/// <summary>
/// Contract for a type whose instances can project themselves to a <typeparamref name="TDestination"/>.
/// </summary>
public interface IMapsTo<out TDestination>
{
    /// <summary>Creates a new <typeparamref name="TDestination"/> populated from this instance.</summary>
    TDestination MapTo();
}
```

**File `src/Common/RECO.Mapping/IScalarCopyable.cs`:**

```csharp
namespace RECO.Mapping;

/// <summary>
/// Contract for scalar (non-navigation) cloning and in-place copying.
/// Designed for ORM scenarios where values must be copied onto an already-tracked
/// entity without touching navigation/relationship properties.
/// </summary>
public interface IScalarCopyable<TSelf> where TSelf : IScalarCopyable<TSelf>
{
    /// <summary>Creates a new instance with this instance's scalar values copied over.</summary>
    TSelf CloneScalars();

    /// <summary>Copies this instance's scalar values onto <paramref name="target"/> in place.</summary>
    void CopyScalarsTo(TSelf target);
}
```

**File `src/Common/RECO.Mapping/IDualMapped.cs`:**

```csharp
namespace RECO.Mapping;

/// <summary>
/// Composite contract for a type that maps bidirectionally with <typeparamref name="TOther"/>
/// and supports scalar cloning / in-place copying.
/// </summary>
public interface IDualMapped<TSelf, TOther> :
    IMappedFrom<TSelf, TOther>, IMapsTo<TOther>, IScalarCopyable<TSelf>
    where TSelf : IDualMapped<TSelf, TOther>
{
}
```

**File `src/Common/RECO.Mapping/MappingExtensions.cs`:**

```csharp
namespace RECO.Mapping;

/// <summary>Collection helpers over the mapping contracts.</summary>
public static class MappingExtensions
{
    /// <summary>Maps every element of <paramref name="sources"/> to a new <typeparamref name="TDest"/> list.</summary>
    public static List<TDest> MapFromAll<TDest, TSource>(this IEnumerable<TSource> sources)
        where TDest : IMappedFrom<TDest, TSource>
        => [.. sources.Select(TDest.MapFrom)];

    /// <summary>Projects every element of <paramref name="sources"/> to a new <typeparamref name="TDest"/> list.</summary>
    public static List<TDest> MapToAll<TSource, TDest>(this IEnumerable<TSource> sources)
        where TSource : IMapsTo<TDest>
        => [.. sources.Select(s => s.MapTo())];
}
```

**File `src/Common/RECO.Mapping/Verification/MappingVerificationException.cs`:**

```csharp
namespace RECO.Mapping.Verification;

/// <summary>Thrown when a mapping fails coverage verification.</summary>
public sealed class MappingVerificationException(string message) : Exception(message);
```

**File `src/Common/RECO.Mapping/Verification/MappingVerifier.cs`:**

```csharp
using System.Collections;
using System.Reflection;

namespace RECO.Mapping.Verification;

/// <summary>
/// Test-time coverage verification for hand-written mappings — the replacement for
/// AutoMapper's <c>AssertConfigurationIsValid()</c>.
/// <para>
/// A source instance is populated with sentinel values via reflection, mapped, and the
/// destination is compared against a freshly constructed baseline:
/// </para>
/// <list type="bullet">
///   <item>Every destination property <b>not</b> listed in <c>unmappedByDesign</c> must differ
///   from its constructor-default value (i.e. the mapping populated it). A property forgotten
///   after a model change fails here.</item>
///   <item>Every property listed in <c>unmappedByDesign</c> must <b>not</b> carry the sentinel
///   value of a same-named source property (i.e. it was not accidentally copied).</item>
/// </list>
/// <para>
/// Reflection is used <b>only here</b>, in test code — production mappings remain
/// plain compiled C#.
/// </para>
/// </summary>
public static class MappingVerifier
{
    /// <summary>
    /// Verifies that <paramref name="map"/> covers every public writable property of
    /// <typeparamref name="TDest"/> except those in <paramref name="unmappedByDesign"/>.
    /// </summary>
    /// <param name="map">The mapping under test.</param>
    /// <param name="unmappedByDesign">
    /// Destination property names intentionally left unmapped (the equivalent of AutoMapper's
    /// <c>ForMember(..., opt => opt.Ignore())</c>).
    /// </param>
    /// <exception cref="MappingVerificationException">Coverage verification failed.</exception>
    public static void AssertAllMembersMapped<TSource, TDest>(
        Func<TSource, TDest> map,
        params string[] unmappedByDesign)
        where TSource : new()
        where TDest : new()
    {
        var ignored = new HashSet<string>(unmappedByDesign, StringComparer.OrdinalIgnoreCase);

        // 1. Build a source instance filled with sentinel values.
        var source = new TSource();
        var sourceProps = typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var sentinels = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in sourceProps.Values)
        {
            if (TryCreateSentinel(prop.PropertyType, prop.Name, out var sentinel))
            {
                prop.SetValue(source, sentinel);
                sentinels[prop.Name] = sentinel;
            }
        }

        // 2. Map, and construct an unmapped baseline to compare constructor defaults against.
        var dest = map(source);
        var baseline = new TDest();

        var failures = new List<string>();
        var destProps = typeof(TDest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var prop in destProps)
        {
            var mappedValue = prop.GetValue(dest);
            var defaultValue = prop.GetValue(baseline);

            if (ignored.Contains(prop.Name))
            {
                // Must NOT have received the sentinel of a same-named source property.
                if (sentinels.TryGetValue(prop.Name, out var sentinel) &&
                    ValuesMatch(mappedValue, sentinel))
                {
                    failures.Add(
                        $"'{prop.Name}' is declared unmapped-by-design but received the source value — remove it from the ignore list or stop assigning it.");
                }
            }
            else if (ValuesMatch(mappedValue, defaultValue))
            {
                failures.Add(
                    $"'{prop.Name}' was not populated by the mapping — assign it, or declare it unmapped-by-design.");
            }
        }

        if (failures.Count > 0)
        {
            throw new MappingVerificationException(
                $"Mapping {typeof(TSource).Name} -> {typeof(TDest).Name} failed verification:{Environment.NewLine}  " +
                string.Join($"{Environment.NewLine}  ", failures));
        }
    }

    private static bool ValuesMatch(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b) || (a is IEnumerable ea && b is IEnumerable eb &&
                               ea.Cast<object?>().SequenceEqual(eb.Cast<object?>()));
    }

    private static bool TryCreateSentinel(Type type, string propertyName, out object? sentinel)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        sentinel = t switch
        {
            _ when t == typeof(string) => $"◆{propertyName}",
            _ when t == typeof(bool) => true,
            _ when t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                   t == typeof(byte) || t == typeof(uint) || t == typeof(ulong) ||
                   t == typeof(ushort) || t == typeof(sbyte)
                => Convert.ChangeType(GetStableSeed(propertyName), t),
            _ when t == typeof(decimal) => (decimal)GetStableSeed(propertyName),
            _ when t == typeof(double) => (double)GetStableSeed(propertyName),
            _ when t == typeof(float) => (float)GetStableSeed(propertyName),
            _ when t == typeof(DateTime) => new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc),
            _ when t == typeof(DateTimeOffset) => new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
            _ when t == typeof(TimeSpan) => TimeSpan.FromMinutes(GetStableSeed(propertyName)),
            _ when t == typeof(Guid) => new Guid("d1a5e0f0-0000-4000-8000-000000000042"),
            _ when t.IsEnum => Enum.GetValues(t).Cast<object>().LastOrDefault(),
            _ => TryCreateReferenceSentinel(t),
        };
        return sentinel is not null;
    }

    private static object? TryCreateReferenceSentinel(Type t)
    {
        // Collections and POCOs: a fresh non-null instance is a sufficient sentinel —
        // reference identity distinguishes "copied" from "not copied".
        try
        {
            return t.IsAbstract || t.IsInterface ? null : Activator.CreateInstance(t);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Small non-zero number derived from the property name, unique enough to catch crossed wires.</summary>
    private static int GetStableSeed(string propertyName) =>
        1 + Math.Abs(propertyName.Aggregate(17, (acc, c) => unchecked(acc * 31 + c))) % 100;
}
```

### ✅ GATE 2

```bash
dotnet build src/Common/RECO.Mapping/RECO.Mapping.csproj
```

Must report **0 errors, 0 warnings**. If it fails, you mistyped one of the files above —
diff your files against this document character by character.

---

