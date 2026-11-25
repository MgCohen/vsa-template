namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Vsa.Definition;
using Vsa = Umpire.Vsa.Definition;

public sealed class RulesArchitecture : VsaArchitecture {
    protected override void Rules(RuleBook rules) {
        rules.For<Vsa.Presenters>().Denies<Vsa.Contracts.Events>();
    }
}

public sealed class AppRulesTests {
    static readonly Dictionary<string, string> NotesSlice = new() { ["Feature"] = "Notes", ["Slice"] = "CreateNote" };

    [Test]
    public void The_VSA_rules_allow_a_use_that_the_app_rule_denies() {
        Assert.That(PresentersUseEvents(new VsaArchitecture()), Is.EqualTo(Permission.Allow));
        Assert.That(PresentersUseEvents(new RulesArchitecture()), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void The_app_rules_come_after_the_VSA_rules() {
        var last = new ResolvedArchitecture(new RulesArchitecture()).UseRules[^1];

        Assert.That(last.Text, Is.EqualTo("Presenters denies Contracts.Events"));
    }

    static Permission PresentersUseEvents(Architecture definition) {
        var resolved = new ResolvedArchitecture(definition);
        var presenters = FeaturePart(resolved, typeof(Vsa.Presenters));
        var events = FeaturePart(resolved, typeof(Vsa.Contracts.Events));
        return resolved.DecisionFor(presenters, NotesSlice, events, NotesSlice).Permission;
    }

    static Part FeaturePart(ResolvedArchitecture resolved, Type layer) =>
        resolved.Parts.Single(p => p.Is(layer) && p.Groups.Any(i => i.Is(typeof(Vsa.Feature))));
}
