namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Vsa.Definition;
using Concepts = Umpire.Vsa.Definition;

public sealed class DerivedRulesTests {
    [Test]
    public void A_derived_architecture_has_its_rules_after_the_VSA_rules() {
        var app = Rules(new RulesArchitecture());
        var vsa = Rules(new VsaArchitecture());

        Assert.That(app.Take(vsa.Count), Is.EqualTo(vsa));
        Assert.That(app.Skip(vsa.Count), Does.Contain(new Rule("Presenters", "Contracts.Events", Permission.Deny)));
    }

    static List<Rule> Rules(Architecture definition) =>
        [.. new ResolvedArchitecture(definition).UseRules.Select(p => new Rule(p.From.Name, p.To.Name, p.Permission))];

    sealed record Rule(string From, string To, Permission Permission);

    sealed class RulesArchitecture : VsaArchitecture {
        protected override void Rules(RuleBook rules) {
            rules.For<Concepts.Presenters>().Denies<Concepts.Contracts.Events>();
        }
    }
}
