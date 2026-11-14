using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using Tests.Shared;
using VSA.Architecture.Data;

namespace Viewer.Tests.Export;

public sealed class MvvmLoopTests
{
    private const string ViewUsesModel = "GreetingView in View (App/View) uses Greeting in Model (App/Model)";
    private const string ViewModelUsesModel =
        "GreetingViewModel in ViewModel (App/ViewModel) uses Greeting in Model (App/Model)";
    private const string BaseUsesModelBase =
        "ViewModelBase`1 in ViewModel (Engine/ViewModel) uses ModelBase in Model (Engine/Model)";

    private static readonly Term View = new("View", []);
    private static readonly Term ViewModel = new("ViewModel", []);
    private static readonly Term Model = new("Model", []);

    [Test]
    public void The_rule_break_gives_the_same_error_with_the_code_and_the_data_definition()
    {
        IReadOnlyList<string> code = MvvmApp.Errors(MvvmApp.Engine());
        IReadOnlyList<string> data = MvvmApp.Errors(MvvmApp.Check(MvvmApp.ArchitectureJson));

        Assert.That(code, Has.Count.EqualTo(1));
        Assert.That(code[0], Does.Contain("error VSA2001").And.Contain(ViewUsesModel));
        Assert.That(data, Is.EqualTo(code));
    }

    [Test]
    public void The_viewer_shows_the_exported_definition_and_the_rule_break()
    {
        Definition definition = ArchitectureImport.Read(MvvmApp.ArchitectureJson);
        LayoutData app = AppGraph.Of(new InputFiles(MvvmApp.SnapshotJson, MvvmApp.ArchitectureJson));

        Assert.That(definition.Parts.Select(p => p.Layer).Distinct(),
            Is.SupersetOf(new[] { "Engine", "App", "Model", "ViewModel", "View" }));
        Assert.That(definition.Rules.Count(r => r.Froms.Count > 0), Is.EqualTo(4));
        Assert.That(app.Nodes.SelectMany(n => n.Rows).Select(r => r.Label),
            Is.SupersetOf(new[] { "GreetingView", "GreetingViewModel", "Greeting" }));
        Assert.That(app.Edges, Has.Some.Matches<Edge>(e => e.Access == Access.Deny));
    }

    [Test]
    public void A_rule_that_the_viewer_adds_allows_the_break()
    {
        string edited = Edit(MvvmApp.ArchitectureJson, new RuleChange(RuleOp.AddRule, View, Model, Access.Allow));

        DotnetRun run = MvvmApp.Check(edited);

        Assert.That(run.ExitCode, Is.Zero, run.Output);
        Assert.That(MvvmApp.Errors(run), Is.Empty);
    }

    [Test]
    public void A_rule_that_the_viewer_removes_gives_a_new_error()
    {
        Definition definition = ArchitectureImport.Read(MvvmApp.ArchitectureJson);
        Rule viewModelRule = definition.Rules
            .Single(r => r.Froms.Any(f => f.Layer == "ViewModel") && r.Tos.Any(t => t.Layer == "Model"));
        RuleChange remove = new(RuleOp.RemoveRule, ViewModel, Model, Access.Allow, viewModelRule.Id);

        IReadOnlyList<string> errors = MvvmApp.Errors(MvvmApp.Check(Edit(MvvmApp.ArchitectureJson, remove)));

        Assert.That(errors, Has.Count.EqualTo(3));
        Assert.That(errors, Has.One.Contains(ViewUsesModel));
        Assert.That(errors, Has.One.Contains(ViewModelUsesModel));
        Assert.That(errors, Has.One.Contains(BaseUsesModelBase));
    }

    [Test]
    public void An_edited_file_imports_back_and_a_second_edit_gives_the_break_again()
    {
        string allowed = Edit(MvvmApp.ArchitectureJson, new RuleChange(RuleOp.AddRule, View, Model, Access.Allow));
        Rule added = ArchitectureImport.Read(allowed).Rules
            .Single(r => r.Froms.Any(f => f.Layer == "View") && r.Tos.Any(t => t.Layer == "Model"));

        string removed = Edit(allowed, new RuleChange(RuleOp.RemoveRule, View, Model, Access.Allow, added.Id));

        Assert.That(MvvmApp.Errors(MvvmApp.Check(allowed)), Is.Empty);
        Assert.That(MvvmApp.Errors(MvvmApp.Check(removed)), Is.EqualTo(MvvmApp.Errors(MvvmApp.Engine())));
    }

    [Test]
    public void The_engine_exports_a_new_snapshot_with_an_edited_file()
    {
        string edited = Edit(MvvmApp.ArchitectureJson, new RuleChange(RuleOp.AddRule, View, Model, Access.Allow));
        string output = TempFolder.Create();

        MvvmApp.Check(edited, "--export", output);

        SnapshotFile snapshot = SnapshotJson.Read(File.ReadAllText(Path.Combine(output, DataFiles.Snapshot("MvvmApp", "Mvvm"))));
        Assert.That(snapshot.Architecture.Name, Is.EqualTo("Mvvm"));
        Assert.That(snapshot.Architecture.Source, Is.EqualTo(DataFiles.Architecture("edited")));
        Assert.That(snapshot.Architecture.Hash, Is.EqualTo(DefinitionJson.Hash(DefinitionJson.Read(edited))));
        Assert.That(snapshot.App.Findings, Is.Empty);
        Assert.That(snapshot.App.Dependencies.Select(d => d.Access), Has.None.EqualTo(Access.Deny));
    }

    [Test]
    public void The_viewer_warns_when_the_snapshot_is_from_another_version_of_the_architecture()
    {
        string edited = Edit(MvvmApp.ArchitectureJson, new RuleChange(RuleOp.AddRule, View, Model, Access.Allow));
        string output = TempFolder.Create();
        MvvmApp.Check(edited, "--export", output);
        string newSnapshot = File.ReadAllText(Path.Combine(output, DataFiles.Snapshot("MvvmApp", "Mvvm")));

        Assert.That(SnapshotCheck.Warning(MvvmApp.SnapshotJson, MvvmApp.ArchitectureJson), Is.Null);
        Assert.That(SnapshotCheck.Warning(MvvmApp.SnapshotJson, edited), Does.Contain("another version of the architecture Mvvm"));
        Assert.That(SnapshotCheck.Warning(newSnapshot, edited), Is.Null);
    }

    private static string Edit(string json, RuleChange change)
    {
        return DefinitionChanges.Apply(json, ArchitectureImport.Read(json), [change]);
    }
}
