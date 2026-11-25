namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class ReferenceTrustTests {
    const string Store = "namespace Sample;\npublic class Store : Base;\n";

    [Test]
    public void A_project_that_did_not_compile_against_the_saved_reference_gives_a_warning() {
        using var build = Built();
        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));

        build.Write("Ui/Base.cs", "namespace Sample;\npublic interface IStore;\npublic class Base : IStore;\n");
        build.Analyze(build.Compile("Ui", "Ui/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_changed_method_body_of_the_reference_does_not_lower_the_finding() {
        using var build = Built();

        build.Write("Ui/Base.cs", "namespace Sample;\n\npublic class Base { public int Run() { return 2; } }\n");
        build.Analyze(build.Compile("Ui", "Ui/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    [Test]
    public void A_new_file_lowers_the_finding_of_a_saved_type() {
        using var build = Built();

        build.Write("Data/Store.Part.cs", "namespace Sample;\npublic partial class Store;\n");

        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void An_older_file_that_no_project_compiles_does_not_lower_a_finding() {
        using var build = new SampleBuild("Ui", "Data");
        build.Write("Data/Excluded.cs", "class Excluded;");

        Fill(build);

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    [Test]
    public void Two_saved_projects_that_reference_each_other_do_not_stop_the_job() {
        using var build = Built();
        var ui = build.Saved("Ui");
        build.State.Save(ui with { References = [.. ui.References, new ProjectReference("Data", "old")] });

        Assert.That(CheckJob.Run(build.State).Succeeded, Is.True);
        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_new_config_file_lowers_an_empty_block_place() {
        using var build = new SampleBuild();
        var place = Samples.Part(Samples.Concept("Pack"), "Packs") with { IsGroup = true, IsBlockPlace = true };
        build.Save(Samples.Layers with { Parts = [.. Samples.Layers.Parts, place] });
        Directory.CreateDirectory(Path.Combine(build.Block, "Packs", "New"));

        build.Write("Packs/New/.umpireconfig", "");

        Assert.That(CheckJob.Run(build.State).Findings.Single(f => f.Code == RuleCodes.EmptyBlockPlace).Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_generated_base_with_no_file_does_not_lower_the_finding() {
        using var build = new SampleBuild("Data");
        build.Save(Definition, Samples.Settings("Data"));
        build.Write("Data/Store.cs", Store);
        build.Write("obj/Base.cs", "namespace Sample;\npublic class Base;\n");

        build.Analyze(build.Compile("Data", "Data/Store.cs", "obj/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    static Definition Definition => Samples.Layers with {
        Parts = [Samples.Layers.Parts[0], Samples.Layers.Parts[1] with { Types = [Samples.Name("IStore")] }],
        Uses = [new UseRule(false, [Samples.Entry(Samples.Data)], Permission.Allow, [Samples.Entry(Samples.Ui)],
            [new UsePair(1, 0, Access.Public, [], [])])],
    };

    static SampleBuild Built() {
        var build = new SampleBuild("Ui", "Data");
        Fill(build);
        return build;
    }

    static void Fill(SampleBuild build) {
        build.Save(Definition);
        build.Write("Ui/Base.cs", "namespace Sample;\npublic class Base { public int Run() { return 1; } }\n");
        build.Write("Data/Store.cs", Store);
        var ui = build.Compile("Ui", "Ui/Base.cs");
        build.Analyze(ui);
        build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));
    }

    static Severity WrongType(SampleBuild build) =>
        CheckJob.Run(build.State).Findings.Single(f => f.Code == RuleCodes.WrongType).Severity;
}
