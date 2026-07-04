namespace ExplicitMapping.Verification;

/// <summary>Thrown when a mapping fails coverage verification.</summary>
public sealed class MappingVerificationException(string message) : Exception(message);
