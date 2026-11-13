using VSA.Architecture;

namespace Architecture.Tests;

public record User : Layer;

public record Target : Layer;

public record OnlyTarget : Layer {
    protected override void Configure(LayerOptions options) {
        options.Depth = Depth.Self;
    }
}

public record WideTarget : Layer {
    protected override void Configure(LayerOptions options) {
        options.Depth = Depth.WithChildren;
    }
}

public record TargetChild : Layer;

public sealed class DepthDefinition<TTarget>(Depth options, Depth? method) : ArchitectureDefinition where TTarget : Layer, new() {
    public bool UserUsesTheChild => Resolve().Any(p => p.From.Is(typeof(User)) && p.To.Is(typeof(TargetChild)));

    protected override void Configure(Options configured) {
        configured.Own = Own.None;
        configured.Depth = options;
    }

    protected override void Define(Root root) {
        root.Add<User>();
        root.Add<TTarget>().Add<TargetChild>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<TTarget>(method);
    }
}

public sealed class DepthTests {
    [TestCase(Depth.WithChildren, ExpectedResult = true)]
    [TestCase(Depth.Self, ExpectedResult = false)]
    public bool With_no_other_reach_the_options_decide(Depth options) {
        return new DepthDefinition<Target>(options, null).UserUsesTheChild;
    }

    [TestCase(Depth.WithChildren, ExpectedResult = false)]
    public bool The_layer_body_wins_over_the_options_Only(Depth options) {
        return new DepthDefinition<OnlyTarget>(options, null).UserUsesTheChild;
    }

    [TestCase(Depth.Self, ExpectedResult = true)]
    public bool The_layer_body_wins_over_the_options_WithChildren(Depth options) {
        return new DepthDefinition<WideTarget>(options, null).UserUsesTheChild;
    }

    [Test]
    public void The_method_parameter_wins_over_the_layer_body() {
        Assert.That(new DepthDefinition<OnlyTarget>(Depth.Self, Depth.WithChildren).UserUsesTheChild, Is.True);
        Assert.That(new DepthDefinition<WideTarget>(Depth.WithChildren, Depth.Self).UserUsesTheChild, Is.False);
    }
}
