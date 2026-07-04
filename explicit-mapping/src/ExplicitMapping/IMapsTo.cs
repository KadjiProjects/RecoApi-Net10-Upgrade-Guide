namespace ExplicitMapping;

/// <summary>
/// Contract for a type whose instances can project themselves to a <typeparamref name="TDestination"/>.
/// </summary>
public interface IMapsTo<out TDestination>
{
    /// <summary>Creates a new <typeparamref name="TDestination"/> populated from this instance.</summary>
    TDestination MapTo();
}
