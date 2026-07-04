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
