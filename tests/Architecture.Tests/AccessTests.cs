using ArchUnitNET.Loader;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace EngineCore.Tests;

public record OpenTarget : Concept;

public record ClosedTarget : Concept {
    protected override void Configure(Options defaults) {
        defaults.Access = Access.Internal;
    }
}

public class VisibilityShapes {
    public int PublicMember() => PrivateProtectedMember() + PrivateMember();

    protected internal int ProtectedInternalMember() => 0;

    protected int ProtectedMember() => 0;

    internal int InternalMember() => 0;

    private protected int PrivateProtectedMember() => 0;

    int PrivateMember() => 0;

    internal static ArchUnitNET.Domain.IType Type() =>
        new ArchLoader().LoadAssemblies(typeof(VisibilityShapes).Assembly).Build().Types
            .Single(t => t.FullName == typeof(VisibilityShapes).FullName);
}

public sealed class AccessArchitecture<TTarget>(Access options, Access? method) : Architecture where TTarget : Concept, new() {
    public Access UserAccess => Resolve().Single(p => p.From.Is(typeof(User)) && p.To.Is(typeof(TTarget))).Access;

    protected override void Configure(Options defaults) {
        defaults.Access = options;
    }

    protected override void Define(Root root) {
        root.Add<User>();
        root.Add<TTarget>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<TTarget>(method);
    }
}

public sealed class AccessTests {
    [TestCase(Access.Internal, Access.Public, ExpectedResult = true)]
    [TestCase(Access.Internal, Access.Internal, ExpectedResult = true)]
    [TestCase(Access.Public, Access.Public, ExpectedResult = true)]
    [TestCase(Access.Public, Access.Internal, ExpectedResult = false)]
    public bool An_access_is_within_a_limit_that_is_the_same_or_wider(Access access, Access limit) {
        return access.IsWithin(limit);
    }

    [TestCase(Access.Public, Access.Internal, ExpectedResult = Access.Internal)]
    [TestCase(Access.Internal, Access.Public, ExpectedResult = Access.Internal)]
    [TestCase(Access.Public, Access.Public, ExpectedResult = Access.Public)]
    public Access The_narrowest_access_is_the_smaller_value(Access first, Access second) {
        return first.Narrowest(second);
    }

    [TestCase("PublicMember", ExpectedResult = Access.Public)]
    [TestCase("ProtectedInternalMember", ExpectedResult = Access.Public)]
    [TestCase("ProtectedMember", ExpectedResult = Access.Public)]
    [TestCase("InternalMember", ExpectedResult = Access.Internal)]
    [TestCase("PrivateProtectedMember", ExpectedResult = Access.Internal)]
    [TestCase("PrivateMember", ExpectedResult = Access.Internal)]
    public Access A_member_visibility_maps_to_internal_or_public(string member) {
        return Exposures.Of(VisibilityShapes.Type(), _ => false).First(e => e.Member.StartsWith(member + "(", StringComparison.Ordinal)).Access;
    }

    [Test]
    public void With_no_other_access_the_options_decide() {
        Assert.That(new AccessArchitecture<OpenTarget>(Access.Internal, null).UserAccess, Is.EqualTo(Access.Internal));
    }

    [Test]
    public void With_no_options_set_the_access_is_public() {
        Assert.That(Options.BuiltIn.Access, Is.EqualTo(Access.Public));
    }

    [Test]
    public void The_layer_body_wins_over_the_options() {
        Assert.That(new AccessArchitecture<ClosedTarget>(Access.Public, null).UserAccess, Is.EqualTo(Access.Internal));
    }

    [Test]
    public void The_method_parameter_wins_over_the_layer_body() {
        Assert.That(new AccessArchitecture<ClosedTarget>(Access.Internal, Access.Public).UserAccess, Is.EqualTo(Access.Public));
    }
}
