using System.Collections;
using System.Reflection;

namespace ExplicitMapping.Verification;

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
