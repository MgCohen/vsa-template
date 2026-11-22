namespace EngineCore.Tests;

using Umpire.Architecture;

public record BaseKind : Concept;

public record DerivedKind : BaseKind;

public sealed class DerivedKindArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Deny;
    }

    protected override void Define(Part root) {
        root.Add<DerivedKind>();
        root.Add<User>();
        root.Add<Target>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<BaseKind>().Uses<Target>();
        rules.For<User>().Uses<BaseKind>();
    }
}

public sealed class DerivedConceptTests {
    static readonly ResolvedArchitecture Resolved = new(new DerivedKindArchitecture());

    [Test]
    public void A_rule_for_a_base_concept_covers_a_part_of_a_derived_concept() {
        Assert.That(Decide<DerivedKind, Target>(), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_target_of_a_base_concept_covers_a_part_of_a_derived_concept() {
        Assert.That(Decide<User, DerivedKind>(), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_rule_for_a_base_concept_does_not_cover_an_unrelated_part() {
        Assert.That(Decide<User, Target>(), Is.EqualTo(Permission.Deny));
    }

    static Permission Decide<TFrom, TTo>() where TFrom : Concept where TTo : Concept {
        var none = new Dictionary<string, string>();
        return Resolved.DecisionFor(PartOf<TFrom>(), none, PartOf<TTo>(), none).Permission;
    }

    static Part PartOf<T>() where T : Concept => Resolved.Parts.Single(p => p.Concept is T);
}
