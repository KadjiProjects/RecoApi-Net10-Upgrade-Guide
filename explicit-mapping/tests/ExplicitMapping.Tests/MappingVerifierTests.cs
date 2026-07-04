using NUnit.Framework;
using ExplicitMapping.Verification;

namespace ExplicitMapping.Tests;

/// <summary>
/// Proves <see cref="MappingVerifier"/> — the replacement for AutoMapper's
/// <c>AssertConfigurationIsValid()</c> — actually catches what it claims to catch:
/// a property the mapping forgot to assign, and a property accidentally copied despite
/// being declared unmapped-by-design. This is the exact pattern the migration guide's
/// per-pair coverage tests use (see §6, verification); the types here are demo stand-ins
/// so this library has no dependency on any application's domain code.
/// </summary>
[TestFixture]
public class MappingVerifierTests
{
    private sealed class Source
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        /// <summary>A source-only field that must never leak into the destination.</summary>
        public string Secret { get; set; } = "";
    }

    private sealed class Destination
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Secret { get; set; } = "";
    }

    [Test]
    public void A_complete_correct_mapping_passes_verification()
    {
        static Destination Map(Source s) => new() { Name = s.Name, Age = s.Age };

        Assert.DoesNotThrow(() =>
            MappingVerifier.AssertAllMembersMapped<Source, Destination>(Map, nameof(Destination.Secret)));
    }

    [Test]
    public void A_property_the_mapping_forgot_to_assign_fails_verification()
    {
        // Age is never assigned — simulates a property added to the model and forgotten
        // in the hand-written mapping. This is the exact class of bug AutoMapper's
        // AssertConfigurationIsValid() used to catch, and hand-written mappings need
        // an equivalent safety net for.
        static Destination Map(Source s) => new() { Name = s.Name };

        var ex = Assert.Throws<MappingVerificationException>(() =>
            MappingVerifier.AssertAllMembersMapped<Source, Destination>(Map, nameof(Destination.Secret)));

        Assert.That(ex!.Message, Does.Contain("Age"));
    }

    [Test]
    public void A_property_declared_unmapped_by_design_but_accidentally_copied_fails_verification()
    {
        // Secret is declared unmapped-by-design below (it's in the ignore list), but the
        // mapping copies it anyway — simulates a copy-paste mistake that would otherwise
        // leak a field the design explicitly wanted excluded.
        static Destination Map(Source s) => new() { Name = s.Name, Age = s.Age, Secret = s.Secret };

        var ex = Assert.Throws<MappingVerificationException>(() =>
            MappingVerifier.AssertAllMembersMapped<Source, Destination>(Map, nameof(Destination.Secret)));

        Assert.That(ex!.Message, Does.Contain("Secret"));
    }
}
