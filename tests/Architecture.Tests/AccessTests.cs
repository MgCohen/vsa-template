namespace EngineCore.Tests;

using Facts.Infrastructure;
using Model.Infrastructure;
using Umpire.Architecture;

public record OpenTarget : Concept;

public record ClosedTarget : Concept {
    protected override void Configure(PartOptions defaults) {
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

    internal static Umpire.CheckModel.TypeFacts Type() {
        var dll = typeof(VisibilityShapes).Assembly.Location;
        var facts = new DllSource().Read(new DllQuery([dll], []));
        return DllSnapshots.Of(facts, Path.GetDirectoryName(dll)!).Single().Files.SelectMany(f => f.Types)
            .Single(t => t.Name.FullName == typeof(VisibilityShapes).FullName);
    }
}

public sealed class AccessArchitecture<TTarget>(Access options, Access? method) : Architecture where TTarget : Concept, new() {
    public Access UserAccess => new ResolvedArchitecture(this).UseRules.Single(p => p.From.Is(typeof(User)) && p.To.Is(typeof(TTarget))).Access;

    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.Access = options;
    }

    protected override void Define(Part block, Part kernel) {
        block.Add<User>();
        block.Add<TTarget>();
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
        var access = Umpire.CheckModel.Exposure.AccessOf(VisibilityShapes.Type().Content.Members.First(m => m.Name == member).Access);
        return Enum.Parse<Access>(access.ToString());
    }

    [Test]
    public void With_no_other_access_the_options_decide() {
        Assert.That(new AccessArchitecture<OpenTarget>(Access.Internal, null).UserAccess, Is.EqualTo(Access.Internal));
    }

    [Test]
    public void With_no_options_set_the_access_is_public() {
        Assert.That(PartOptions.BuiltIn.Access, Is.EqualTo(Access.Public));
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
