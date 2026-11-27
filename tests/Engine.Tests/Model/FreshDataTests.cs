namespace CheckModel.Tests;

using Microsoft.CodeAnalysis;
using Umpire.CheckModel;

public sealed class FreshDataTests {
    const string Screen = "namespace Sample;\npublic class Screen;\n";
    const string Store = "namespace Sample;\npublic class Store {\n    public object Make() => new Screen();\n}\n";

    static Definition Counted => Samples.Layers with {
        Counts = [new CountRule(
            [Samples.Entry(Samples.Data)], [1], false, true, false, false, new CountedTypes(false, [0], Samples.Ui), false, Min: 1)],
    };

    [Test]
    public void A_changed_file_of_the_same_project_lowers_a_use() {
        using var build = Built();
        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Error }));

        build.Write("Data/Other.cs", "namespace Sample;\npublic class Other { }\n");

        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Warning }));
    }

    [Test]
    public void A_changed_file_of_a_referenced_project_lowers_a_use() {
        using var build = Built();

        build.Write("Ui/Factory.cs", "namespace Sample;\npublic class Factory { }\n");

        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Warning }));
    }

    [Test]
    public void A_definition_that_is_not_the_one_of_the_block_file_lowers_a_use() {
        using var build = Built();

        build.State.Save(Samples.Layers with { Name = "Other" });

        var report = CheckJob.Run(build.State);
        Assert.That(report.HasErrors, Is.False);
        Assert.That(report.Findings.Select(f => f.Code), Has.Member(DataCodes.OldSnapshot));
    }

    [Test]
    public void A_type_that_is_not_in_the_old_snapshot_of_its_project_lowers_a_match() {
        using var build = new SampleBuild("Ui", "Data");
        build.Save(Samples.Layers with { Matches = [new MatchRule([Samples.Entry(Samples.Data)], [1], Samples.Ui, [0], [])] });
        build.Write("Ui/Screen.cs", Screen);
        build.Analyze(build.Compile("Ui", "Ui/Screen.cs"));
        build.Write("Ui/Request.cs", "namespace Sample;\npublic class Request;\n");
        build.Write("Data/Handler.cs", "namespace Sample;\npublic interface IHandler<T>;\npublic class Handler : IHandler<Request>;\n");

        var diagnostics = build.Analyze(build.Compile("Data", [build.Compile("Ui", "Ui/Screen.cs", "Ui/Request.cs")], "Data/Handler.cs"));

        Assert.That(diagnostics.Where(d => d.Id == RuleCodes.Match).Select(d => d.Severity),
            Is.Not.Empty.And.All.EqualTo(DiagnosticSeverity.Warning));
    }

    [Test]
    public void A_file_that_is_in_no_snapshot_lowers_a_count() {
        using var build = new SampleBuild();
        build.Save(Counted);
        build.Write("Data/Store.cs", "namespace Sample;\npublic class Store;\n");
        build.Analyze(build.Compile("Sample", "Data/Store.cs"));
        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Error }));

        build.Write("Tests/Tests.csproj", "");
        build.Write("Tests/Test.cs", "class Test;");
        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Error }));

        build.Write("Data/New.cs", "class New;");
        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Warning }));
    }

    [Test]
    public void A_folder_with_a_new_file_lowers_an_empty_block_place() {
        using var build = new SampleBuild();
        var place = Samples.Part(Samples.Concept("Pack"), "Packs") with { IsGroup = true, IsBlockPlace = true };
        build.Save(Samples.Layers with { Parts = [.. Samples.Layers.Parts, place] });
        Directory.CreateDirectory(Path.Combine(build.Block, "Packs", "New"));
        build.Write("Ui/Screen.cs", Screen);
        build.Analyze(build.Compile("Sample", "Ui/Screen.cs"));
        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Error }));

        build.Write("Packs/New/NewPack.cs", "class NewPack;");

        Assert.That(Severities(build), Is.EqualTo(new[] { Severity.Warning }));
    }

    [Test]
    public void A_type_that_fits_two_parts_is_a_finding_and_the_job_runs_to_its_end() {
        using var build = new SampleBuild();
        var parts = new[] { "First", "Second" }.Select(n =>
            Samples.Part(Samples.Concept(n), "") with { Folder = null, Types = [Samples.Name("Screen")] }).ToArray();
        build.Save(Samples.Layers with { Parts = [Samples.Part(Samples.Ui, "Ui", parts), Samples.Layers.Parts[1]], Uses = [] });
        build.Write("Ui/Screen.cs", Screen);
        build.Analyze(build.Compile("Sample", "Ui/Screen.cs"));

        var report = CheckJob.Run(build.State);

        Assert.That(report.Succeeded, Is.True);
        Assert.That(report.Findings.Single().Text, Does.Contain("matches First and Second"));
    }

    [Test]
    public void The_block_file_of_another_state_must_be_current() {
        using var build = Built();
        using var other = new TempState();
        other.State.Save(Samples.Settings("Elsewhere") with { Inputs = [new FileMark("config", "0", 0)] });
        other.State.Save(Snapshot.Of("Elsewhere", []));

        build.Save(Samples.Layers, Samples.Settings("Ui", "Data") with { OtherStates = [BlockPath.Of(build.State.Folder, other.State.Folder)] });

        Assert.That(CheckJob.Run(build.State).Findings.Select(f => f.Code), Has.Member(DataCodes.NoSnapshot));
    }

    [Test]
    public void A_new_file_in_a_marked_folder_makes_the_mark_old() {
        using var temp = new TempState();
        var folder = Directory.CreateDirectory(Path.Combine(temp.Block, "Definition")).FullName;
        File.WriteAllText(Path.Combine(folder, "A.cs"), "");
        var mark = FileMark.Of(temp.Block, folder);
        Assert.That(mark.IsCurrent(temp.Block), Is.True);

        File.WriteAllText(Path.Combine(folder, "B.cs"), "");

        Assert.That(mark.IsCurrent(temp.Block), Is.False);
    }

    static SampleBuild Built() {
        var build = new SampleBuild("Ui", "Data");
        build.Write("Ui/Screen.cs", Screen);
        build.Write("Ui/Factory.cs", "namespace Sample;\npublic class Factory;\n");
        build.Write("Data/Store.cs", Store);
        build.Write("Data/Other.cs", "namespace Sample;\npublic class Other;\n");
        var ui = build.Compile("Ui", "Ui/Screen.cs", "Ui/Factory.cs");
        build.Analyze(ui);
        build.Analyze(build.Compile("Data", [ui], "Data/Store.cs", "Data/Other.cs"));
        return build;
    }

    static IEnumerable<Severity> Severities(SampleBuild build) => CheckJob.Run(build.State).Findings
        .Where(f => RuleCodes.All.Contains(f.Code)).Select(f => f.Severity);
}
