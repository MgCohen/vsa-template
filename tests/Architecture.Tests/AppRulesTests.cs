using System.Reflection;
using VSA.Architecture;
using VSA.Architecture.Engine;
using VSA.Architecture.Vsa;
using Vsa = VSA.Architecture.Vsa;

namespace Architecture.Tests;

public sealed class RulesApp : IArchitectureSource {
    public ArchitectureDefinition Architecture => new VsaDefinition(map => map.For<Vsa.Presenters>().Denies<Vsa.Contracts.Events>());
}

public sealed class AppRulesTests {
    static readonly string FixtureRoot = typeof(AppRulesTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(a => a.Key == DiscoveredApp.AppRootKey).Value!;

    static readonly Dictionary<string, string> NotesSlice = new() { ["Feature"] = "Notes", ["Slice"] = "CreateNote" };

    [Test]
    public void Discovery_reads_the_rules_of_the_app_before_the_definitions() {
        var app = DiscoveredApp.FromFolder(AppContext.BaseDirectory, typeof(ArchitectureDefinition), FixtureRoot);

        Assert.That(PresentersUseEvents(app.Definition), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void Discovery_of_a_named_definition_does_not_read_the_app() {
        var app = DiscoveredApp.FromFolder(AppContext.BaseDirectory, typeof(GoodDefinition), FixtureRoot);

        Assert.That(app.Definition, Is.TypeOf<GoodDefinition>());
    }

    [Test]
    public void The_VSA_rules_allow_a_use_that_the_app_rule_denies() {
        Assert.That(PresentersUseEvents(new VsaDefinition()), Is.EqualTo(Permission.Allow));
        Assert.That(PresentersUseEvents(new RulesApp().Architecture), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void The_app_rules_come_after_the_VSA_rules() {
        var last = new RulesApp().Architecture.Resolve()[^1];

        Assert.That((last.From.Name, last.To.Name, last.Permission), Is.EqualTo(("Presenters", "Contracts.Events", Permission.Deny)));
    }

    static Permission PresentersUseEvents(ArchitectureDefinition definition) {
        var presenters = FeaturePart(definition, typeof(Vsa.Presenters));
        var events = FeaturePart(definition, typeof(Vsa.Contracts.Events));
        return Decide.For(definition, presenters, NotesSlice, events, NotesSlice).Permission;
    }

    static Part FeaturePart(ArchitectureDefinition definition, Type layer) =>
        definition.Parts().Single(p => p.Is(layer) && p.Items.Any(i => i.Is(typeof(Vsa.Feature))));
}
