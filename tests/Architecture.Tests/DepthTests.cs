using Umpire.Architecture;

namespace EngineCore.Tests;

public record User : Concept;

public record Target : Concept;

public record OnlyTarget : Concept {
    protected override void Configure(Options defaults) {
        defaults.Depth = Depth.Self;
    }
}

public record WideTarget : Concept {
    protected override void Configure(Options defaults) {
        defaults.Depth = Depth.WithChildren;
    }
}

public record TargetChild : Concept;

public sealed class DepthArchitecture<TTarget>(Depth options, Depth? method) : Architecture where TTarget : Concept, new() {
    public bool UserUsesTheChild => Resolve().Any(p => p.From.Is(typeof(User)) && p.To.Is(typeof(TargetChild)));

    protected override void Configure(Options defaults) {
        defaults.Depth = options;
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
        return new DepthArchitecture<Target>(options, null).UserUsesTheChild;
    }

    [TestCase(Depth.WithChildren, ExpectedResult = false)]
    public bool The_layer_body_wins_over_the_options_Only(Depth options) {
        return new DepthArchitecture<OnlyTarget>(options, null).UserUsesTheChild;
    }

    [TestCase(Depth.Self, ExpectedResult = true)]
    public bool The_layer_body_wins_over_the_options_WithChildren(Depth options) {
        return new DepthArchitecture<WideTarget>(options, null).UserUsesTheChild;
    }

    [Test]
    public void The_method_parameter_wins_over_the_layer_body() {
        Assert.That(new DepthArchitecture<OnlyTarget>(Depth.Self, Depth.WithChildren).UserUsesTheChild, Is.True);
        Assert.That(new DepthArchitecture<WideTarget>(Depth.WithChildren, Depth.Self).UserUsesTheChild, Is.False);
    }
}
