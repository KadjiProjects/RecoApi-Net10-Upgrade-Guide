namespace ExplicitMapping;

/// <summary>
/// Contract for scalar (non-navigation) cloning and in-place copying.
/// Designed for ORM scenarios where values must be copied onto an already-tracked
/// entity without touching navigation/relationship properties.
/// </summary>
/// <typeparam name="TSelf">The implementing type (curiously recurring).</typeparam>
public interface IScalarCopyable<TSelf> where TSelf : IScalarCopyable<TSelf>
{
    /// <summary>Creates a new instance with this instance's scalar values copied over.</summary>
    TSelf CloneScalars();

    /// <summary>Copies this instance's scalar values onto <paramref name="target"/> in place.</summary>
    void CopyScalarsTo(TSelf target);
}
