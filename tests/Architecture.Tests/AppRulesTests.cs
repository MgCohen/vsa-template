using VSA.Architecture;
using VSA.Architecture.Engine;
using VSA.Architecture.Vsa;
using Vsa = VSA.Architecture.Vsa;

namespace EngineCore.Tests;

public sealed class RulesArchitecture : VsaArchitecture {
    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
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
        var last = new RulesArchitecture().Resolve()[^1];

        Assert.That(last.From.Name, Is.EqualTo("Presenters"));
        Assert.That(last.To.Name, Is.EqualTo("Contracts.Events"));
        Assert.That(last.Permission, Is.EqualTo(Permission.Deny));
    }

    static Permission PresentersUseEvents(Architecture definition) {
        var presenters = FeaturePart(definition, typeof(Vsa.Presenters));
        var events = FeaturePart(definition, typeof(Vsa.Contracts.Events));
        return Decide.For(definition, presenters, NotesSlice, events, NotesSlice).Permission;
    }

    static Part FeaturePart(Architecture definition, Type layer) =>
        definition.Parts().Single(p => p.Is(layer) && p.Groups.Any(i => i.Is(typeof(Vsa.Feature))));
}
