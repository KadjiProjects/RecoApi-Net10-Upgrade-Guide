namespace ExplicitMapping;

/// <summary>
/// Composite contract for a type that maps bidirectionally with <typeparamref name="TOther"/>
/// and supports scalar cloning / in-place copying.
/// Generic pipelines can constrain on this single interface to gain:
/// <list type="bullet">
///   <item><c>TSelf.MapFrom(other)</c> — construct from the counterpart type</item>
///   <item><c>instance.MapTo()</c> — project back to the counterpart type</item>
///   <item><c>instance.CloneScalars()</c> / <c>instance.CopyScalarsTo(target)</c></item>
/// </list>
/// </summary>
public interface IDualMapped<TSelf, TOther> :
    IMappedFrom<TSelf, TOther>, IMapsTo<TOther>, IScalarCopyable<TSelf>
    where TSelf : IDualMapped<TSelf, TOther>
{
}
