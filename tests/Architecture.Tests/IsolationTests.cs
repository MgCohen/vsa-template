using VSA.Architecture;

namespace Architecture.Tests;

public enum CrossRule { Plain, Open, InOther }

public sealed class IsolationDefinition(Isolation options, Isolation? group, CrossRule rule) : ArchitectureDefinition {
    public Permission Decide(string fromModule, string toModule) {
        var user = Parts().Single(p => p.Is(typeof(User)));
        var target = Parts().Single(p => p.Is(typeof(Target)));
        return VSA.Architecture.Engine.Decide.For(this,
            user, new Dictionary<string, string> { ["Module"] = fromModule },
            target, new Dictionary<string, string> { ["Module"] = toModule }).Permission;
    }

    protected override void Configure(Options configured) {
        configured.Own = Own.None;
        configured.Isolation = options;
    }

    protected override void Define(Root root) {
        var module = group is { } isolation ? root.AddMany<Module>(isolation) : root.AddMany<Module>();
        module.Add<Target>().And<User>();
    }

    protected override void Rules(Map map) {
        var from = map.For<User>();
        switch (rule) {
            case CrossRule.Plain:
                from.Uses<Target>();
                break;
            case CrossRule.Open:
                from.Uses<Target>(Isolation.Open);
                break;
            case CrossRule.InOther:
                from.Uses<Target>().InOther<Module>();
                break;
        }
    }
}

public sealed class IsolationTests {
    [TestCase(Isolation.Open, null, CrossRule.Plain, Permission.Allow)]
    [TestCase(Isolation.Isolated, null, CrossRule.Plain, Permission.Deny)]
    [TestCase(Isolation.Isolated, Isolation.Open, CrossRule.Plain, Permission.Allow)]
    [TestCase(Isolation.Open, Isolation.Isolated, CrossRule.Plain, Permission.Deny)]
    [TestCase(Isolation.Isolated, null, CrossRule.Open, Permission.Allow)]
    [TestCase(Isolation.Isolated, null, CrossRule.InOther, Permission.Allow)]
    public void A_rule_reaches_another_group_item_only_when_the_group_is_open_or_the_rule_crosses_it(
        Isolation options, Isolation? group, CrossRule rule, Permission expected) {
        var definition = new IsolationDefinition(options, group, rule);

        Assert.That(definition.Decide("a", "b"), Is.EqualTo(expected));
    }

    [Test]
    public void An_isolated_group_item_uses_its_own_parts() {
        var definition = new IsolationDefinition(Isolation.Isolated, null, CrossRule.Plain);

        Assert.That(definition.Decide("a", "a"), Is.EqualTo(Permission.Allow));
    }
}
