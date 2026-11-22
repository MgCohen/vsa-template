namespace EngineCore.Tests;

using Umpire.Architecture;

public class BaseLevelArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Deny;
    }

    protected override void Define(Part root) {
        root.Add<User>();
        root.Add<Target>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<Target>();
    }
}

public sealed class DerivedLevelArchitecture : BaseLevelArchitecture {
    protected override void Define(Part root) {
        root.Add<Child>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Child>().Uses<User>();
    }
}

public sealed class DefinitionLevelTests {
    static readonly ResolvedArchitecture Resolved = new(new DerivedLevelArchitecture());

    [Test]
    public void A_derived_definition_keeps_the_parts_of_its_base_with_no_base_call() {
        Assert.That(Resolved.Parts.Select(p => p.Name), Is.EquivalentTo(new[] { "User", "Target", "Child" }));
    }

    [Test]
    public void A_derived_definition_keeps_the_rules_of_its_base_with_no_base_call() {
        Assert.Multiple(() => {
            Assert.That(Decide<User, Target>(), Is.EqualTo(Permission.Allow));
            Assert.That(Decide<Child, User>(), Is.EqualTo(Permission.Allow));
        });
    }

    [Test]
    public void A_derived_definition_keeps_the_options_of_its_base_with_no_base_call() {
        Assert.That(Decide<Target, User>(), Is.EqualTo(Permission.Deny));
    }

    static Permission Decide<TFrom, TTo>() where TFrom : Concept where TTo : Concept {
        var none = new Dictionary<string, string>();
        return Resolved.DecisionFor(PartOf<TFrom>(), none, PartOf<TTo>(), none).Permission;
    }

    static Part PartOf<T>() where T : Concept => Resolved.Parts.Single(p => p.Concept is T);
}
