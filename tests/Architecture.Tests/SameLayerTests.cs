using VSA.Architecture;

namespace Architecture.Tests;

public record Package : Layer;

public sealed class SameLayerDefinition(bool useSameLayer) : ArchitectureDefinition {
    public Permission Decision(string fromPath, string toPath) =>
        Resolve().LastOrDefault(p => p.From.Path == fromPath && p.To.Path == toPath)?.Permission ?? Options.DefaultPermission;

    protected override void Configure(Options configured) {
        configured.UseSameLayer = useSameLayer;
    }

    protected override void Define(Root root) {
        root.Add<Target>();
        root.AddGroup<Package>().Add<Target>().And<User>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<Target>();
    }
}

public sealed class SameLayerTests {
    [Test]
    public void A_part_uses_the_other_parts_of_its_layer_when_the_option_allows_it() {
        var definition = new SameLayerDefinition(true);

        Assert.That(definition.Decision("Package/{package}/Target", "Target"), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_part_does_not_use_the_other_parts_of_its_layer_by_default() {
        var definition = new SameLayerDefinition(false);

        Assert.That(definition.Decision("Package/{package}/Target", "Target"), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_rule_overrides_the_same_layer_option() {
        var definition = new DenyingSameLayerDefinition();

        Assert.That(definition.Decision("Package/{package}/Target", "Target"), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void The_same_layer_option_allows_no_other_layer() {
        var definition = new SameLayerDefinition(true);

        Assert.That(definition.Decision("Package/{package}/Target", "Package/{package}/User"), Is.EqualTo(Permission.Deny));
    }
}

public sealed class DenyingSameLayerDefinition : ArchitectureDefinition {
    public Permission Decision(string fromPath, string toPath) =>
        Resolve().LastOrDefault(p => p.From.Path == fromPath && p.To.Path == toPath)?.Permission ?? Options.DefaultPermission;

    protected override void Configure(Options configured) {
        configured.UseSameLayer = true;
    }

    protected override void Define(Root root) {
        root.Add<Target>();
        root.AddGroup<Package>().Add<Target>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Target>().Denies<Target>();
    }
}
