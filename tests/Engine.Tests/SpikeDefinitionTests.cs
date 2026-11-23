using Spike.Alpha.Definition;
using Spike.Charlie.Definition;
using Umpire.Architecture;

namespace Engine.Tests;

public sealed class SpikeDefinitionTests {
    static readonly string[] alphaFolders = ["Definition", "A1", "A2", "A2/Store", "Bravo/B1", "Bravo/B2"];
    static readonly string[] bravoFolders = ["Bravo/B1", "Bravo/B2"];
    static readonly SpikeView<AlphaArchitecture> alpha = new();
    static readonly SpikeView<CharlieArchitecture> charlie = new();

    [TestCase("A2", "A1", Permission.Allow)]
    [TestCase("A2", "A2/Store", Permission.Allow)]
    [TestCase("A1", "Bravo/B1", Permission.Allow)]
    [TestCase("Bravo/B2", "Bravo/B1", Permission.Allow)]
    [TestCase("A1", "A2", Permission.Deny)]
    [TestCase("A2/Store", "Bravo/B1", Permission.Deny)]
    [TestCase("Bravo/B1", "Bravo/B2", Permission.Deny)]
    [TestCase("Bravo/B1", "A1", Permission.Deny)]
    public void Alpha_gives_the_planned_decision(string from, string to, Permission permission) {
        Assert.That(alpha.Decide(from, to), Is.EqualTo(permission));
    }

    [Test]
    public void The_reused_Bravo_piece_gives_the_same_decisions_in_Alpha_and_Charlie() {
        var pairs = Pairs(bravoFolders);

        Assert.That(pairs.Select(charlie.Decision), Is.EqualTo(pairs.Select(alpha.Decision)));
    }

    [Test]
    public void The_reused_Bravo_piece_binds_to_the_same_parts_in_Alpha_and_Charlie() {
        Assert.That(bravoFolders.Select(charlie.PartAt), Is.EqualTo(bravoFolders.Select(alpha.PartAt)));
        Assert.That(bravoFolders.Select(alpha.PartAt), Is.EqualTo(new[] { "Bravo.B1", "Bravo.B2" }));
    }

    [Test]
    public void Charlie_keeps_each_decision_of_Alpha() {
        var pairs = Pairs(alphaFolders);

        Assert.That(pairs.Select(charlie.Decision), Is.EqualTo(pairs.Select(alpha.Decision)));
    }

    [Test]
    public void The_child_part_Store_gets_the_uses_of_its_parent_A2_in_Alpha_and_Charlie() {
        Assert.That(alpha.Decide("A2/Store", "A1"), Is.EqualTo(Permission.Allow));
        Assert.That(charlie.Decide("A2/Store", "A1"), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void The_local_part_Store_binds_in_Alpha_and_in_Charlie() {
        Assert.That(alpha.PartAt("A2/Store"), Is.EqualTo("AlphaParts.Store"));
        Assert.That(charlie.PartAt("A2/Store"), Is.EqualTo("AlphaParts.Store"));
    }

    [Test]
    public void Charlie_adds_its_own_part_and_rule_and_Alpha_does_not_see_them() {
        Assert.That(charlie.Decide("C1", "A2"), Is.EqualTo(Permission.Allow));
        Assert.That(charlie.Decide("A1", "C1"), Is.EqualTo(Permission.Deny));
        Assert.That(alpha.IsMapped("C1"), Is.False);
    }

    static List<SpikeUse> Pairs(string[] folders) =>
        [.. folders.SelectMany(from => folders.Where(to => to != from).Select(to => new SpikeUse(from, to)))];

    sealed record SpikeUse(string From, string To);

    sealed class SpikeView<TArchitecture> : ArchitectureTest<TArchitecture> where TArchitecture : Architecture, new() {
        public Permission Decide(string from, string to) => MayUse(At(from), At(to)).Permission;

        public string Decision(SpikeUse use) => $"{use.From} -> {use.To}: {Decide(use.From, use.To)}";

        public string PartAt(string folder) => At(folder).Part;

        public bool IsMapped(string folder) => At(folder).IsMapped;
    }
}
