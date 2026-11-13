using VSA.Architecture;

namespace Architecture.Tests;

public record OpenTarget : Layer;

public record ClosedTarget : Layer {
    protected override void Configure(LayerOptions options) {
        options.Access = Access.Private;
    }
}

public sealed class AccessDefinition<TTarget>(Access options, Access? method) : ArchitectureDefinition where TTarget : Layer, new() {
    public Access UserAccess => Resolve().Single(p => p.From.Is(typeof(User)) && p.To.Is(typeof(TTarget))).Access;

    protected override void Configure(Options configured) {
        configured.Access = options;
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
    [TestCase(Access.Private, Access.Public, ExpectedResult = true)]
    [TestCase(Access.Public, Access.Internal, ExpectedResult = false)]
    [TestCase(Access.Internal, Access.ProtectedInternal, ExpectedResult = true)]
    [TestCase(Access.ProtectedInternal, Access.Internal, ExpectedResult = false)]
    [TestCase(Access.PrivateProtected, Access.Protected, ExpectedResult = true)]
    [TestCase(Access.Protected, Access.Internal, ExpectedResult = false)]
    [TestCase(Access.Internal, Access.Protected, ExpectedResult = false)]
    public bool An_access_is_within_a_limit_only_on_both_sides_of_the_assembly(Access access, Access limit) {
        return access.IsWithin(limit);
    }

    [TestCase(Access.Public, Access.Internal, ExpectedResult = Access.Internal)]
    [TestCase(Access.Protected, Access.Internal, ExpectedResult = Access.PrivateProtected)]
    [TestCase(Access.ProtectedInternal, Access.Protected, ExpectedResult = Access.Protected)]
    public Access The_narrowest_access_is_the_smaller_side_of_each(Access first, Access second) {
        return first.Narrowest(second);
    }

    [Test]
    public void With_no_other_access_the_options_decide() {
        Assert.That(new AccessDefinition<OpenTarget>(Access.Internal, null).UserAccess, Is.EqualTo(Access.Internal));
    }

    [Test]
    public void With_no_options_set_the_access_is_public() {
        Assert.That(new Options().Access, Is.EqualTo(Access.Public));
    }

    [Test]
    public void The_layer_body_wins_over_the_options() {
        Assert.That(new AccessDefinition<ClosedTarget>(Access.Public, null).UserAccess, Is.EqualTo(Access.Private));
    }

    [Test]
    public void The_method_parameter_wins_over_the_layer_body() {
        Assert.That(new AccessDefinition<ClosedTarget>(Access.Public, Access.Internal).UserAccess, Is.EqualTo(Access.Internal));
    }
}
