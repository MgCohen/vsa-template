namespace EngineCore.Tests;

using LayeredApp.Ui;
using Umpire.Architecture;
using Rules.Slices.CheckRules;

public record Ui : Concept;

public record Logic : Concept;

public record Data : Concept;

public sealed class LayeredArchitecture : Architecture {
    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.DefaultPermission = Permission.Deny;
    }

    protected override void Define(Part block, Part kernel) {
        block.Add<Ui>();
        block.Add<Logic>();
        block.Add<Data>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Ui>().Uses<Logic>();
        rules.For<Logic>().Uses<Data>();
    }
}

public sealed class LayeredAppTests {
    static readonly Lazy<ArchitectureCheck> LayeredMap =
        new(() => FixtureApps.Load(new LayeredArchitecture(), AppContext.BaseDirectory, FixtureApps.Block("LayeredApp")));

    [Test]
    public void The_engine_checks_an_architecture_with_no_VSA_type() {
        var findings = LayeredMap.Value.Findings().Select(FixtureApps.Line).ToList();

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0], Does.StartWith("LayeredApp/Data/OrderStore.cs").And.Contain("error UMP020101").And.Contain("OrderScreen"));
    }

    [Test]
    public void Each_type_of_the_layered_app_is_in_a_part() {
        Assert.That(LayeredMap.Value.Bindings.Unmapped.Select(u => u.Type.FullName), Is.Empty);
    }

    [Test]
    public void The_layered_app_references_no_VSA_assembly() {
        var references = typeof(OrderScreen).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.That(references, Has.None.StartsWith("Umpire."));
    }
}
