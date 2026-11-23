namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class ActingRuleTests {
    [Test]
    public void The_level_on_top_tightens_a_rule_of_its_base() {
        Assert.That(Decide(new TighteningTop()), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void The_level_on_top_loosens_a_rule_of_its_base() {
        Assert.That(Decide(new LooseningTop()), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void The_decision_takes_its_permission_from_the_acting_rule() {
        var resolved = new ResolvedArchitecture(new TighteningTop());
        var user = resolved.Parts.Single(p => p.Concept is TopUser);
        var target = resolved.Parts.Single(p => p.Concept is TopTarget);
        var none = new Dictionary<string, string>();

        var decision = resolved.DecisionFor(user, none, target, none);

        Assert.That(decision.Permission, Is.EqualTo(resolved.ActingRule(user, none, target, none)!.Permission));
    }

    static Permission Decide(Architecture architecture) {
        var resolved = new ResolvedArchitecture(architecture);
        var user = resolved.Parts.Single(p => p.Concept is TopUser);
        var target = resolved.Parts.Single(p => p.Concept is TopTarget);
        var none = new Dictionary<string, string>();
        return resolved.ActingRule(user, none, target, none)!.Permission;
    }

    public sealed record TopUser : Concept;

    public sealed record TopTarget : Concept;

    public class AllowingBase : Architecture {
        protected override void Define(Part root, Part kernel) => root.Add<TopUser>().And<TopTarget>();

        protected override void Rules(RuleBook rules) => rules.For<TopUser>().Uses<TopTarget>();
    }

    public sealed class TighteningTop : AllowingBase {
        protected override void Rules(RuleBook rules) => rules.For<TopUser>().Denies<TopTarget>();
    }

    public class DenyingBase : Architecture {
        protected override void Configure(ArchitectureConfig config) => config.Defaults.DefaultPermission = Permission.Allow;

        protected override void Define(Part root, Part kernel) => root.Add<TopUser>().And<TopTarget>();

        protected override void Rules(RuleBook rules) => rules.For<TopUser>().Denies<TopTarget>();
    }

    public sealed class LooseningTop : DenyingBase {
        protected override void Rules(RuleBook rules) => rules.For<TopUser>().Uses<TopTarget>();
    }
}
