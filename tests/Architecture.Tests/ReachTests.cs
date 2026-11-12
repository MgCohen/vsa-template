using VSA.Architecture;

namespace Architecture.Tests;

public record User : Layer;

public record Target : Layer;

public record OnlyTarget : Layer {
    protected override void Define(Part part) {
        part.Reaches(Reach.Only);
    }
}

public record WideTarget : Layer {
    protected override void Define(Part part) {
        part.Reaches(Reach.WithChildren);
    }
}

public record TargetChild : Layer;

public sealed class ReachDefinition<TTarget>(Reach options, Reach? method) : ArchitectureDefinition where TTarget : Layer, new() {
    public bool UserReachesTheChild => Resolve().Any(p => p.From.Is(typeof(User)) && p.To.Is(typeof(TargetChild)));

    protected override void Configure(Options configured) {
        configured.Own = Own.None;
        configured.Reach = options;
    }

    protected override void Define(Root root) {
        root.Add<User>();
        root.Add<TTarget>().Add<TargetChild>();
    }

    protected override void Rules(Map map) {
        map.For<User>().Uses<TTarget>(method);
    }
}

public sealed class ReachTests {
    [TestCase(Reach.WithChildren, ExpectedResult = true)]
    [TestCase(Reach.Only, ExpectedResult = false)]
    public bool With_no_other_reach_the_options_decide(Reach options) {
        return new ReachDefinition<Target>(options, null).UserReachesTheChild;
    }

    [TestCase(Reach.WithChildren, ExpectedResult = false)]
    public bool The_layer_body_wins_over_the_options_Only(Reach options) {
        return new ReachDefinition<OnlyTarget>(options, null).UserReachesTheChild;
    }

    [TestCase(Reach.Only, ExpectedResult = true)]
    public bool The_layer_body_wins_over_the_options_WithChildren(Reach options) {
        return new ReachDefinition<WideTarget>(options, null).UserReachesTheChild;
    }

    [Test]
    public void The_method_parameter_wins_over_the_layer_body() {
        Assert.That(new ReachDefinition<OnlyTarget>(Reach.Only, Reach.WithChildren).UserReachesTheChild, Is.True);
        Assert.That(new ReachDefinition<WideTarget>(Reach.WithChildren, Reach.Only).UserReachesTheChild, Is.False);
    }
}
