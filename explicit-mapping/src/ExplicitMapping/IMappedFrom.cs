namespace ExplicitMapping;

/// <summary>
/// Contract for a type that can construct itself from a <typeparamref name="TSource"/>.
/// The static abstract member makes the factory callable from generic code
/// (<c>TSelf.MapFrom(source)</c>) with full compile-time checking and zero reflection.
/// </summary>
/// <typeparam name="TSelf">The implementing type (curiously recurring).</typeparam>
/// <typeparam name="TSource">The type mapped from.</typeparam>
public interface IMappedFrom<TSelf, in TSource> where TSelf : IMappedFrom<TSelf, TSource>
{
    /// <summary>Creates a new <typeparamref name="TSelf"/> populated from <paramref name="source"/>.</summary>
    static abstract TSelf MapFrom(TSource source);
}
