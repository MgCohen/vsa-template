using Spike.Alpha.Definition;
using Spike.Bravo.Definition;
using Spike.Charlie.Definition;
using Umpire.Architecture;

namespace Engine.Tests;

public sealed class SpikeDefinitionTests {
    static readonly string[] alphaFolders = ["Definition", "A1", "A2", "A2/Store", "Zulu/Z1", "Zulu/Z2", "Plugins/Echo/B1"];
    static readonly string[] zuluFolders = ["Zulu/Z1", "Zulu/Z2"];
    static readonly string[] pluginFolders = ["Plugins/Echo/B1", "Zulu/Z1", "Zulu/Z2"];
    static readonly SpikeView<AlphaArchitecture> alpha = new();
    static readonly SpikeView<BravoArchitecture> bravo = new();
    static readonly SpikeView<CharlieArchitecture> charlie = new();

    [Test]
    public void A_root_places_the_shared_Zulu_block_and_the_Bravo_plugin() {
        Assert.Multiple(() => {
            Assert.That(zuluFolders.Select(alpha.PartAt), Is.EqualTo(new[] { "Zulu.Z1", "Zulu.Z2" }));
            Assert.That(alpha.PartAt("Plugins/Echo/B1"), Is.EqualTo("BravoArchitecture.B1"));
            Assert.That(zuluFolders.Select(bravo.PartAt), Is.EqualTo(new[] { "Zulu.Z1", "Zulu.Z2" }));
        });
    }

    [Test]
    public void Another_root_cannot_place_the_owned_concept_B1() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new OwnedB1Architecture()))!;

        Assert.That(error.Message, Does.Contain("BravoArchitecture.B1 belongs to BravoArchitecture"));
    }

    [TestCase("A2", "A1", Permission.Allow)]
    [TestCase("A2", "A2/Store", Permission.Allow)]
    [TestCase("A1", "Zulu/Z1", Permission.Allow)]
    [TestCase("A1", "Plugins/Echo/B1", Permission.Allow)]
    [TestCase("Plugins/Echo/B1", "A1", Permission.Allow)]
    [TestCase("Plugins/Echo/B1", "Zulu/Z1", Permission.Allow)]
    [TestCase("Zulu/Z2", "Zulu/Z1", Permission.Allow)]
    [TestCase("A1", "A2", Permission.Deny)]
    [TestCase("A2", "Zulu/Z1", Permission.Deny)]
    [TestCase("Zulu/Z1", "Zulu/Z2", Permission.Deny)]
    [TestCase("Zulu/Z1", "A1", Permission.Deny)]
    public void Alpha_gives_the_planned_decision(string from, string to, Permission permission) {
        Assert.That(alpha.Decide(from, to), Is.EqualTo(permission));
    }

    [Test]
    public void The_Zulu_block_gives_the_same_decisions_in_each_root() {
        var pairs = Pairs(zuluFolders);

        Assert.Multiple(() => {
            Assert.That(pairs.Select(bravo.Decision), Is.EqualTo(pairs.Select(alpha.Decision)));
            Assert.That(pairs.Select(charlie.Decision), Is.EqualTo(pairs.Select(alpha.Decision)));
        });
    }

    [Test]
    public void The_Bravo_plugin_gives_the_same_decisions_in_Alpha_and_Bravo() {
        var pairs = Pairs(pluginFolders);

        Assert.That(pairs.Select(alpha.Decision), Is.EqualTo(pairs.Select(bravo.Decision)));
    }

    [Test]
    public void Charlie_keeps_each_decision_of_Alpha() {
        var pairs = Pairs(alphaFolders);

        Assert.That(pairs.Select(charlie.Decision), Is.EqualTo(pairs.Select(alpha.Decision)));
    }

    [Test]
    public void The_child_part_Store_gets_the_uses_of_its_parent_A2() {
        Assert.Multiple(() => {
            Assert.That(alpha.Decide("A2/Store", "A1"), Is.EqualTo(Permission.Allow));
            Assert.That(charlie.Decide("A2/Store", "A1"), Is.EqualTo(Permission.Allow));
            Assert.That(charlie.PartAt("A2/Store"), Is.EqualTo(alpha.PartAt("A2/Store")));
        });
    }

    [Test]
    public void Charlie_adds_its_own_part_and_rule_and_Alpha_does_not_see_them() {
        Assert.Multiple(() => {
            Assert.That(charlie.Decide("C1", "A2"), Is.EqualTo(Permission.Allow));
            Assert.That(charlie.Decide("A1", "C1"), Is.EqualTo(Permission.Deny));
            Assert.That(alpha.IsMapped("C1"), Is.False);
        });
    }

    [Test]
    public void An_overlay_in_Charlie_adds_a_part_inside_each_placed_Zulu_block() {
        Assert.Multiple(() => {
            Assert.That(charlie.PartAt("Zulu/Z3"), Is.EqualTo("CharlieParts.Z3"));
            Assert.That(charlie.Decide("Zulu/Z3", "Zulu/Z1"), Is.EqualTo(Permission.Allow));
            Assert.That(alpha.PartAt("Zulu/Z3"), Is.EqualTo("Zulu"));
        });
    }

    static List<SpikeUse> Pairs(string[] folders) =>
        [.. folders.SelectMany(from => folders.Where(to => to != from).Select(to => new SpikeUse(from, to)))];

    sealed record SpikeUse(string From, string To);

    sealed class OwnedB1Architecture : Architecture {
        protected override void Define(Part root, Part kernel) => root.Add<BravoArchitecture.B1>();
    }

    sealed class SpikeView<TArchitecture> : ArchitectureTest<TArchitecture> where TArchitecture : Architecture, new() {
        public Permission Decide(string from, string to) => MayUse(At(from), At(to)).Permission;

        public string Decision(SpikeUse use) => $"{use.From} -> {use.To}: {Decide(use.From, use.To)}";

        public string PartAt(string folder) => At(folder).Part;

        public bool IsMapped(string folder) => At(folder).IsMapped;
    }
}
