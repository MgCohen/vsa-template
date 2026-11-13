using VSA.Architecture;

namespace Architecture.Tests;

public enum CrossRule { Plain, Open, InOther }

public sealed class BoundaryDefinition(Boundary options, Boundary? group, CrossRule rule) : ArchitectureDefinition {
    public Permission Decide(string fromPackage, string toPackage) {
        var user = Parts().Single(p => p.Is(typeof(User)));
        var target = Parts().Single(p => p.Is(typeof(Target)));
        return VSA.Architecture.Engine.Decide.For(this,
            user, new Dictionary<string, string> { ["Package"] = fromPackage },
            target, new Dictionary<string, string> { ["Package"] = toPackage }).Permission;
    }

    protected override void Configure(Options configured) {
        configured.Boundary = options;
    }

    protected override void Define(Root root) {
        var package = group is { } boundary ? root.AddGroup<Package>(boundary) : root.AddGroup<Package>();
        package.Add<Target>().And<User>();
    }

    protected override void Rules(RuleBook rules) {
        var from = rules.For<User>();
        switch (rule) {
            case CrossRule.Plain:
                from.Uses<Target>();
                break;
            case CrossRule.Open:
                from.Uses<Target>(Boundary.Open);
                break;
            case CrossRule.InOther:
                from.Uses<Target>().InOther<Package>();
                break;
        }
    }
}

public sealed class BoundaryTests {
    [TestCase(Boundary.Open, null, CrossRule.Plain, Permission.Allow)]
    [TestCase(Boundary.Closed, null, CrossRule.Plain, Permission.Deny)]
    [TestCase(Boundary.Closed, Boundary.Open, CrossRule.Plain, Permission.Allow)]
    [TestCase(Boundary.Open, Boundary.Closed, CrossRule.Plain, Permission.Deny)]
    [TestCase(Boundary.Closed, null, CrossRule.Open, Permission.Allow)]
    [TestCase(Boundary.Closed, null, CrossRule.InOther, Permission.Allow)]
    public void A_rule_reaches_another_group_instance_only_when_the_group_is_open_or_the_rule_crosses_it(
        Boundary options, Boundary? group, CrossRule rule, Permission expected) {
        var definition = new BoundaryDefinition(options, group, rule);

        Assert.That(definition.Decide("a", "b"), Is.EqualTo(expected));
    }

    [Test]
    public void A_closed_group_instance_uses_its_own_parts() {
        var definition = new BoundaryDefinition(Boundary.Closed, null, CrossRule.Plain);

        Assert.That(definition.Decide("a", "a"), Is.EqualTo(Permission.Allow));
    }
}
