namespace EngineCore.Tests;

using Umpire.Architecture;

public enum CrossRule { Plain, Open, InOther }

public sealed class BoundaryArchitecture(Boundary options, Boundary? group, CrossRule rule) : Architecture {
    public Permission MayUse(string fromPackage, string toPackage) {
        var resolved = new ResolvedArchitecture(this);
        var user = resolved.Parts.Single(p => p.Is(typeof(User)));
        var target = resolved.Parts.Single(p => p.Is(typeof(Target)));
        return resolved.DecisionFor(
            user, new Dictionary<string, string> { ["Package"] = fromPackage },
            target, new Dictionary<string, string> { ["Package"] = toPackage }).Permission;
    }

    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.Boundary = options;
    }

    protected override void Define(Part block, Part kernel) {
        var package = group is { } boundary ? block.AddGroup<Package>(boundary) : block.AddGroup<Package>();
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
        var definition = new BoundaryArchitecture(options, group, rule);

        Assert.That(definition.MayUse("a", "b"), Is.EqualTo(expected));
    }

    [Test]
    public void A_closed_group_instance_uses_its_own_parts() {
        var definition = new BoundaryArchitecture(Boundary.Closed, null, CrossRule.Plain);

        Assert.That(definition.MayUse("a", "a"), Is.EqualTo(Permission.Allow));
    }

    [TestCase(Boundary.Open, CrossRule.Plain, "always")]
    [TestCase(Boundary.Closed, CrossRule.Plain, "same Package")]
    [TestCase(Boundary.Closed, CrossRule.InOther, "other Package")]
    public void The_condition_of_a_rule_names_its_groups(Boundary options, CrossRule rule, string expected) {
        var resolved = new ResolvedArchitecture(new BoundaryArchitecture(options, null, rule));

        Assert.That(resolved.UseRules.Single().Condition.ToString(), Is.EqualTo(expected));
    }

    [TestCase(false, "same Package, other Group")]
    [TestCase(true, "other Package, same Group")]
    public void The_condition_text_keeps_the_order_of_the_filters(bool otherPackage, string expected) {
        var definition = new InlineRulesArchitecture(
            block => block.AddGroup<Package>().AddGroup<Group>().Add<Target>().And<User>(),
            rules => {
                var uses = rules.For<User>().Uses<Target>();
                if (otherPackage) {
                    uses.InOther<Package>().InSame<Group>();
                } else {
                    uses.InOther<Group>();
                }
            });

        Assert.That(new ResolvedArchitecture(definition).UseRules.Single().Condition.ToString(), Is.EqualTo(expected));
    }


    [Test]
    public void A_filter_after_And_applies_to_each_earlier_target() {
        var definition = new InlineRulesArchitecture(
            block => {
                block.AddGroup<Package>().Add<Target>().And<User>();
                block.Add<Group>().Add<Child>();
            },
            rules => rules.For<User>().Uses<Target>().Under<Package>().And<Child>().Under<Group>());

        Assert.That(() => new ResolvedArchitecture(definition), Throws.InvalidOperationException.With.Message.Contains(
            "the target Target Under<Package> Under<Group> matches no part"));
    }
}

public sealed class InlineRulesArchitecture(Action<Part> define, Action<RuleBook> rules) : Architecture {
    protected override void Define(Part block, Part kernel) {
        define(block);
    }

    protected override void Rules(RuleBook book) {
        rules(book);
    }
}
