namespace CheckModel.Tests;

using Microsoft.CodeAnalysis;
using Umpire.CheckModel;

public sealed class TrustTests {
    const string Request = "namespace Sample;\npublic class Request;\n";
    const string Handler = "namespace Sample;\npublic interface IHandler<T>;\npublic class Handler : IHandler<Request>;\n";

    static Definition Matched => Samples.Layers with {
        Matches = [new MatchRule([Samples.Entry(Samples.Data)], [1], Samples.Ui, [0], [])],
    };

    static Definition Counted => Samples.Layers with {
        Counts = [new CountRule(
            [Samples.Entry(Samples.Data)], [1], false, true, false, false,
            new CountedTypes(false, [0], Samples.Ui), false, Min: 1)],
    };

    [Test]
    public void A_match_with_a_missing_snapshot_is_a_warning_in_the_analyzer() {
        using var build = new SampleBuild("Ui", "Data");
        build.Save(Matched);
        build.Write("Ui/Request.cs", Request);
        build.Write("Data/Handler.cs", Handler);

        var diagnostics = build.Analyze(build.Compile("Data", [build.Compile("Ui", "Ui/Request.cs")], "Data/Handler.cs"));

        Assert.That(diagnostics.Where(d => d.Id == RuleCodes.Match).Select(d => d.Severity),
            Is.Not.Empty.And.All.EqualTo(DiagnosticSeverity.Warning));
    }

    [Test]
    public void A_match_and_a_count_with_a_missing_snapshot_are_warnings_in_the_command() {
        using var build = new SampleBuild("Ui", "Data");
        build.Save(Matched with { Counts = Counted.Counts });
        build.Write("Ui/Request.cs", Request);
        build.Write("Data/Handler.cs", Handler);
        build.Analyze(build.Compile("Data", [build.Compile("Ui", "Ui/Request.cs")], "Data/Handler.cs"));

        var report = ModelCheck.Run(build.State);

        Assert.That(report.Findings.Select(f => f.Code), Has.Member(RuleCodes.Match).And.Member(RuleCodes.Count));
        Assert.That(report.HasErrors, Is.False);
    }

    [Test]
    public void A_use_of_an_old_type_is_a_warning_in_the_command() {
        using var build = Wrong();
        Assert.That(ModelCheck.Run(build.State).HasErrors, Is.True);
        build.Write("Ui/Screen.cs", "namespace Sample;\n\npublic class Screen;\n");

        var report = ModelCheck.Run(build.State);

        Assert.That(report.Findings.Where(f => f.Code == RuleCodes.DeniedUse).Select(f => f.Severity), Is.EqualTo(new[] { Severity.Warning }));
    }

    [Test]
    public void An_old_type_of_another_state_lowers_its_finding_and_is_reported() {
        using var build = Wrong();
        using var other = new TempState();
        other.State.Save(Samples.Settings("Ui"));
        File.Move(build.State.SnapshotFile("Ui"), Target(other.State.SnapshotFile("Ui")));
        build.Save(Samples.Layers, Samples.Settings("Data") with { OtherStates = [BlockPath.Of(build.State.Folder, other.State.Folder)] });

        var report = ModelCheck.Run(build.State);

        Assert.That(report.HasErrors, Is.False);
        Assert.That(report.Findings.Select(f => f.Code), Has.Member(DataCodes.OldSnapshot));
    }

    [Test]
    public void A_type_with_an_old_base_type_gives_a_warning() {
        using var build = new SampleBuild("Ui", "Data");
        build.Save(Samples.Layers with { Parts = [Samples.Layers.Parts[0], Samples.Layers.Parts[1] with { Types = [Samples.Name("IStore")] }] });
        build.Write("Ui/Base.cs", "namespace Sample;\npublic class Base;\n");
        build.Write("Data/Store.cs", "namespace Sample;\npublic class Store : Base;\n");
        var ui = build.Compile("Ui", "Ui/Base.cs");
        build.Analyze(ui);
        Assert.That(build.Analyze(build.Compile("Data", [ui], "Data/Store.cs")).Single(d => d.Id == RuleCodes.WrongType).Severity, Is.EqualTo(DiagnosticSeverity.Error));
        build.Write("Ui/Base.cs", "namespace Sample;\npublic class Base { }\n");

        var diagnostics = build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));

        Assert.That(diagnostics.Where(d => d.Id == RuleCodes.WrongType).Select(d => d.Severity), Is.EqualTo(new[] { DiagnosticSeverity.Warning }));
    }

    [Test]
    public void A_helper_with_an_old_user_gives_a_warning() {
        using var build = new SampleBuild("First", "Second");
        var parent = Samples.Part(Samples.Data, "Data", Samples.Part(Samples.Ui, "Ui") with { Folder = null, Types = [Samples.Name("IUser")] });
        build.Save(Samples.Layers with { Parts = [parent with { Types = [Samples.Name("INothing")] }], Uses = [] });
        build.Write("Data/Helper.cs", "namespace Sample;\npublic class Helper;\n");
        build.Write("Data/User.cs", "namespace Sample;\npublic interface IUser;\npublic class User : IUser;\n");
        build.Analyze(build.Compile("Second", "Data/User.cs"));
        build.Write("Data/User.cs", "namespace Sample;\npublic interface IUser;\npublic class User : IUser { }\n");

        var diagnostics = build.Analyze(build.Compile("First", "Data/Helper.cs"));

        Assert.That(diagnostics.Where(d => d.Id == RuleCodes.WrongType).Select(d => d.Severity), Is.EqualTo(new[] { DiagnosticSeverity.Warning }));
    }

    [Test]
    public void A_missing_project_under_an_External_gives_a_warning() {
        using var build = new SampleBuild("Sample.Ui", "Data");
        var external = Samples.Part(Samples.Concept("Library"), "Library") with { External = "Sample" };
        build.Save(Samples.Layers with { Parts = [.. Samples.Layers.Parts, external] });
        build.Write("Ui/Screen.cs", "namespace Sample;\npublic class Screen;\n");
        build.Write("Data/Store.cs", "namespace Other;\npublic class Store {\n    public object Make() => new Sample.Screen();\n}\n");

        var diagnostics = build.Analyze(build.Compile("Data", [build.Compile("Sample.Ui", "Ui/Screen.cs")], "Data/Store.cs"));

        Assert.That(diagnostics.Where(d => d.Id == RuleCodes.DeniedUse).Select(d => d.Severity), Is.EqualTo(new[] { DiagnosticSeverity.Warning }));
    }

    [TestCase("missing")]
    [TestCase("old")]
    public void A_missing_or_old_library_gives_a_warning(string state) {
        using var build = new SampleBuild("Data");
        var external = Samples.Part(Samples.Concept("Library"), "Library") with { External = "Kit" };
        build.Save(Samples.Layers with { Parts = [.. Samples.Layers.Parts, external] }, Samples.Settings("Data") with { Libraries = ["Kit"] });
        var dll = build.Write("Kit.dll", "first");
        build.State.SaveLibrary(Snapshot.Of("Kit", []) with { Source = FileMark.Of(build.Block, dll) });
        build.Write("Kit/Screen.cs", "namespace Sample;\npublic class Screen;\n");
        build.Write("Data/Store.cs", "namespace Other;\npublic class Store {\n    public object Make() => new Sample.Screen();\n}\n");
        var compiled = build.Compile("Data", [build.Compile("Kit", "Kit/Screen.cs")], "Data/Store.cs");
        Assert.That(build.Analyze(compiled).Where(d => d.Id == RuleCodes.DeniedUse).Select(d => d.Severity), Is.EqualTo(new[] { DiagnosticSeverity.Error }));
        if (state == "missing") {
            File.Delete(build.State.LibraryFiles.Single());
        } else {
            build.Write("Kit.dll", "second");
        }

        var data = build.Compile("Data", [build.Compile("Kit", "Kit/Screen.cs")], "Data/Store.cs");

        Assert.That(build.Analyze(data).Where(d => d.Id == RuleCodes.DeniedUse).Select(d => d.Severity), Is.EqualTo(new[] { DiagnosticSeverity.Warning }));
        Assert.That(ModelCheck.Run(build.State).HasErrors, Is.False);
    }

    [TestCase("Definition/Layers.cs")]
    [TestCase(".umpireconfig")]
    public void An_old_definition_or_block_file_gives_warnings(string changed) {
        using var build = Wrong();
        build.Save(Samples.Layers, Samples.Settings("Ui", "Data") with {
            DefinitionSources = [FileMark.Of(build.Block, build.Write("Definition/Layers.cs", "first"))],
            Inputs = [FileMark.Of(build.Block, Path.Combine(build.Block, ".umpireconfig"))],
        });
        Assert.That(ModelCheck.Run(build.State).HasErrors, Is.True);

        build.Write(changed, "second");
        var report = ModelCheck.Run(build.State);

        Assert.That(report.Findings.Where(f => f.Code == RuleCodes.DeniedUse).Select(f => f.Severity), Is.EqualTo(new[] { Severity.Warning }));
        Assert.That(report.Findings.Select(f => f.Code), Has.Member(DataCodes.OldSnapshot).And.Member(DataCodes.NotComplete));
        Assert.That(report.IsComplete, Is.False);
    }

    [Test]
    public void A_block_file_with_no_mark_of_the_definition_gives_warnings() {
        using var build = Wrong();
        build.Save(Samples.Layers, Samples.Settings("Ui", "Data") with { DefinitionSources = [] });

        Assert.That(ModelCheck.Run(build.State).HasErrors, Is.False);
    }

    [Test]
    public void The_mode_Warn_gives_warnings_and_the_mode_of_the_caller_wins() {
        using var build = Wrong();
        build.Save(Samples.Layers, Samples.Settings("Ui", "Data") with { Mode = CheckMode.Warn });

        Assert.That(ModelCheck.Run(build.State).HasErrors, Is.False);
        Assert.That(ModelCheck.Run(build.State, CheckMode.Error).HasErrors, Is.True);
    }

    static SampleBuild Wrong() {
        var build = new SampleBuild("Ui", "Data");
        build.Write("Ui/Screen.cs", "namespace Sample;\npublic class Screen;\n");
        build.Write("Data/Store.cs", "namespace Sample;\npublic class Store {\n    public object Make() => new Screen();\n}\n");
        var ui = build.Compile("Ui", "Ui/Screen.cs");
        build.Analyze(ui);
        build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));
        return build;
    }

    static string Target(string file) {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        return file;
    }
}
