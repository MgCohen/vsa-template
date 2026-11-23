namespace EngineCore.Tests;

using Umpire.Architecture;

public record Package : Concept;

public sealed class SameConceptArchitecture(bool useSameConcept) : Architecture {
    public Permission Decision(string fromPath, string toPath) {
        var resolved = new ResolvedArchitecture(this);
        var sameGroups = resolved.Parts.Where(p => p.IsGroup).Select(p => p.Name).Distinct().ToDictionary(name => name, _ => "one");
        return resolved.ActingRule(resolved.Parts.First(p => p.Path == fromPath), sameGroups, resolved.Parts.First(p => p.Path == toPath), sameGroups)
            ?.Permission ?? resolved.Options.DefaultPermission!.Value;
    }

    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.UseSameConcept = useSameConcept;
    }

    protected override void Define(Part block, Part kernel) {
        block.Add<Target>();
        block.AddGroup<Package>().Add<Target>().And<User>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<Target>();
    }
}

public sealed class SameConceptTests {
    [Test]
    public void A_part_uses_the_other_parts_of_its_layer_when_the_option_allows_it() {
        var definition = new SameConceptArchitecture(true);

        Assert.That(definition.Decision("Package/{package}/Target", "Target"), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_part_does_not_use_the_other_parts_of_its_layer_by_default() {
        var definition = new SameConceptArchitecture(false);

        Assert.That(definition.Decision("Package/{package}/Target", "Target"), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_rule_overrides_the_same_layer_option() {
        var definition = new DenyingSameLayerArchitecture();

        Assert.That(definition.Decision("Package/{package}/Target", "Target"), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void The_same_layer_option_allows_no_other_layer() {
        var definition = new SameConceptArchitecture(true);

        Assert.That(definition.Decision("Package/{package}/Target", "Package/{package}/User"), Is.EqualTo(Permission.Deny));
    }
}

public sealed class DenyingSameLayerArchitecture : Architecture {
    public Permission Decision(string fromPath, string toPath) {
        var resolved = new ResolvedArchitecture(this);
        var sameGroups = resolved.Parts.Where(p => p.IsGroup).Select(p => p.Name).Distinct().ToDictionary(name => name, _ => "one");
        return resolved.ActingRule(resolved.Parts.First(p => p.Path == fromPath), sameGroups, resolved.Parts.First(p => p.Path == toPath), sameGroups)
            ?.Permission ?? resolved.Options.DefaultPermission!.Value;
    }

    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.UseSameConcept = true;
    }

    protected override void Define(Part block, Part kernel) {
        block.Add<Target>();
        block.AddGroup<Package>().Add<Target>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Target>().Denies<Target>();
    }
}
