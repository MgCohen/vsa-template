namespace CheckModel.Tests;

using Microsoft.CodeAnalysis;
using Umpire.CheckModel;

public sealed class AnalyzerTests {
    const string Screen = "namespace Sample;\npublic class Screen;\n";
    const string Store = "namespace Sample;\npublic class Store {\n    public object Make() => new Screen();\n}\n";

    [Test]
    public void A_wrong_use_in_one_project_is_an_error_at_the_use() {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", Screen);
        build.Write("Data/Store.cs", Store);

        var diagnostic = build.Analyze(build.Compile("Sample", "Ui/Screen.cs", "Data/Store.cs")).Single();

        Assert.That((diagnostic.Id, diagnostic.Severity), Is.EqualTo(("UMP020101", DiagnosticSeverity.Error)));
        Assert.That(diagnostic.Location.GetLineSpan().StartLinePosition.Line, Is.EqualTo(2));
    }

    [TestCase("public string Name() => nameof(Screen);", "public class Screen;")]
    [TestCase("public int Size() => Screen.Max;", "public class Screen { public const int Max = 1; }")]
    [TestCase("public object Shade() => Screen.Dark;", "public enum Screen { Dark }")]
    public void A_name_a_constant_and_an_enum_value_are_a_use(string member, string screen) {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", $"namespace Sample;\n{screen}\n");
        build.Write("Data/Store.cs", $"namespace Sample;\npublic class Store {{\n    {member}\n}}\n");

        var diagnostic = build.Analyze(build.Compile("Sample", "Ui/Screen.cs", "Data/Store.cs")).Single();

        Assert.That((diagnostic.Id, diagnostic.Location.GetLineSpan().StartLinePosition.Line), Is.EqualTo(("UMP020101", 2)));
        Assert.That(diagnostic.GetMessage(), Does.StartWith("Store in Data (Data) uses Screen in Ui (Ui)"));
    }

    [Test]
    public void A_build_saves_the_snapshot_and_the_command_gives_the_same_finding() {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", Screen);
        build.Write("Data/Store.cs", Store);

        var diagnostic = build.Analyze(build.Compile("Sample", "Ui/Screen.cs", "Data/Store.cs")).Single();
        var finding = StateCheck.Run(build.State).Findings.Single();

        Assert.That(finding.Text, Is.EqualTo(diagnostic.GetMessage()));
        Assert.That(finding.Line, Is.EqualTo(diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1));
        Assert.That(build.Saved().Files.Select(f => f.Path), Is.EqualTo(new[] { "Data/Store.cs", "Ui/Screen.cs" }));
    }

    [TestCase("class Screen : Missing;")]
    [TestCase("class Screen : IMissing<Other>, Missing { int a, ; }")]
    [TestCase("enum Screen { A, , B }")]
    [TestCase("class Screen { void Run() { new Missing().Go(; } }")]
    public void A_compile_error_saves_no_snapshot(string type) {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", $"namespace Sample;\npublic {type}\n");

        build.Analyze(build.Compile("Sample", "Ui/Screen.cs"));

        Assert.That(build.State.SnapshotFiles, Is.Empty);
    }

    [TestCase("build_property.DesignTimeBuild", "true")]
    [TestCase("build_property.UmpireSaveSnapshot", "false")]
    public void An_editor_saves_no_snapshot(string option, string value) {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", Screen);
        build.Options[option] = value;

        build.Analyze(build.Compile("Sample", "Ui/Screen.cs"));

        Assert.That(build.State.SnapshotFiles, Is.Empty);
    }

    [Test]
    public void A_project_that_is_not_in_the_block_file_is_not_checked() {
        using var build = new SampleBuild("Other");
        build.Write("Tools/Clock.cs", Screen);

        Assert.That(build.Analyze(build.Compile("Sample", "Tools/Clock.cs")), Is.Empty);
        Assert.That(build.State.SnapshotFiles, Is.Empty);
    }

    [Test]
    public void A_block_with_no_definition_is_not_checked() {
        using var build = new SampleBuild();
        build.Write("Tools/Clock.cs", Screen);
        File.Delete(build.State.DefinitionFile);

        Assert.That(build.Analyze(build.Compile("Sample", "Tools/Clock.cs")), Is.Empty);
    }

    [Test]
    public void A_bad_definition_is_a_warning() {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", Screen);
        File.WriteAllText(build.State.DefinitionFile, "{");

        var diagnostic = build.Analyze(build.Compile("Sample", "Ui/Screen.cs")).Single();

        Assert.That((diagnostic.Id, diagnostic.Severity), Is.EqualTo(("UMP030801", DiagnosticSeverity.Warning)));
    }

    [Test]
    public void A_partial_type_gives_the_finding_in_the_file_of_the_use() {
        using var build = new SampleBuild();
        build.Write("Ui/Screen.cs", Screen);
        build.Write("Data/Store.cs", "namespace Sample;\npublic partial class Store;\n");
        build.Write("Data/Store.Make.cs", Store.Replace("class Store", "partial class Store"));

        var diagnostic = build.Analyze(build.Compile("Sample", "Ui/Screen.cs", "Data/Store.cs", "Data/Store.Make.cs")).Single();

        Assert.That(diagnostic.Location.SourceTree?.FilePath, Does.EndWith("Store.Make.cs"));
        Assert.That(build.Saved().Files.SelectMany(f => f.Types).Count(t => t.Name.Name == "Store"), Is.EqualTo(2));
    }

    [Test]
    public void A_use_of_a_current_type_of_another_project_is_an_error() {
        using var build = new SampleBuild("Ui", "Data");

        Assert.That(UseOfAnotherProject(build).Single().Severity, Is.EqualTo(DiagnosticSeverity.Error));
    }

    [Test]
    public void A_use_of_an_old_type_of_another_project_is_a_warning() {
        using var build = new SampleBuild("Ui", "Data");

        var diagnostics = UseOfAnotherProject(build, () => build.Write("Ui/Screen.cs", Screen + "\n"));

        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { RuleCodes.DeniedUse, DataCodes.OldSnapshot }));
        Assert.That(diagnostics.Select(d => d.Severity), Is.All.EqualTo(DiagnosticSeverity.Warning));
    }

    [Test]
    public void A_missing_snapshot_of_a_referenced_project_is_a_warning() {
        using var build = new SampleBuild("Ui", "Data");

        var diagnostics = UseOfAnotherProject(build, () => File.Delete(build.State.SnapshotFile("Ui")));

        Assert.That(diagnostics.Select(d => (d.Id, d.Severity)), Is.EqualTo(new[] { ("UMP030802", DiagnosticSeverity.Warning) }));
    }

    [Test]
    public void A_moved_file_is_found_by_the_last_job_with_no_compile_of_the_user() {
        using var build = new SampleBuild("Ui", "Data");
        build.Write("Ui/Screen.cs", Screen);
        build.Write("Data/Store.cs", Store);
        var ui = build.Compile("Ui", "Ui/Screen.cs");
        build.Analyze(ui);
        build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));
        File.Delete(Path.Combine(build.Block, "Ui", "Screen.cs"));
        build.Write("Data/Screen.cs", Screen);
        build.Analyze(build.Compile("Ui", "Data/Screen.cs"));
        Assert.That(StateCheck.Run(build.State).Findings, Is.Empty);
        File.Delete(Path.Combine(build.Block, "Data", "Screen.cs"));
        build.Write("Ui/Screen.cs", Screen);
        build.Analyze(build.Compile("Ui", "Ui/Screen.cs"));

        var finding = StateCheck.Run(build.State).Findings.Single();
        Assert.That((finding.Code, finding.Severity), Is.EqualTo((RuleCodes.DeniedUse, Severity.Error)));
    }

    static IReadOnlyList<Diagnostic> UseOfAnotherProject(SampleBuild build, Action? change = null) {
        build.Write("Ui/Screen.cs", Screen);
        build.Write("Data/Store.cs", Store);
        var ui = build.Compile("Ui", "Ui/Screen.cs");
        build.Analyze(ui);
        change?.Invoke();
        return build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));
    }
}
