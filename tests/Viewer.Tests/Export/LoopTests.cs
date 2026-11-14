using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using Tests.Shared;

namespace Viewer.Tests.Export;

public sealed class LoopTests
{
    [Test]
    public void The_exported_definition_of_Notes_checks_Notes_with_no_errors()
    {
        DotnetRun run = NotesExport.Engine("--definition", NotesExport.ArchitectureFile);

        Assert.That(run.ExitCode, Is.Zero, run.Output);
        Assert.That(run.Output, Does.Not.Contain(": error "));
    }

    [Test]
    public void A_definition_without_its_allow_rules_gives_errors_when_the_engine_checks_Notes()
    {
        Definition definition = ArchitectureImport.Read(NotesExport.ArchitectureJson);
        List<RuleChange> removed = [.. definition.Rules.Where(r => r.Access == Access.Allow)
            .SelectMany(r => r.Froms.SelectMany(f => r.Tos.Select(t => new RuleChange(RuleOp.RemoveRule, f, t, r.Access, r.Id))))];
        string edited = DefinitionChanges.Apply(NotesExport.ArchitectureJson, definition, removed);
        string file = Path.Combine(TempFolder.Create(), "edited.architecture.json");
        File.WriteAllText(file, edited);

        DotnetRun run = NotesExport.Engine("--definition", file);

        Assert.That(ArchitectureImport.Read(edited).Rules.Where(r => r.Access == Access.Allow && r.Froms.Count > 0), Is.Empty);
        Assert.That(run.ExitCode, Is.EqualTo(1), run.Output);
        Assert.That(run.Output, Does.Contain(": error VSA2"));
    }

    [Test]
    public void A_changed_access_and_an_added_rule_import_back_with_the_same_rules()
    {
        Definition definition = ArchitectureImport.Read(NotesExport.ArchitectureJson);
        Rule denied = definition.Rules.First(r => r.Access == Access.Deny && r.Froms.Count == 1 && r.Tos.Count == 1);
        Term presenters = new("Presenters", []);
        Term infrastructure = new("Infrastructure", [new Condition(Condition.InSame, "Feature")]);
        RuleChange[] changes =
        [
            new(RuleOp.ChangeAccess, denied.Froms[0], denied.Tos[0], Access.Allow, denied.Id),
            new(RuleOp.AddRule, presenters, infrastructure, Access.Allow),
        ];

        Definition edited = ArchitectureImport.Read(DefinitionChanges.Apply(NotesExport.ArchitectureJson, definition, changes));

        Assert.That(edited.Rules.Select(r => r.Text), Does.Not.Contain(denied.Text));
        Assert.That(edited.Rules, Has.Some.Matches<Rule>(r => r.Access == Access.Allow && r.Froms.Count == 1 && r.Tos.Count == 1
            && r.Froms.Single().Layer == denied.Froms[0].Layer && r.Tos.Single().Layer == denied.Tos[0].Layer));
        Assert.That(edited.Rules, Has.Some.Matches<Rule>(r => r.Froms.Count == 1 && r.Froms[0].Layer == "Presenters"
            && r.Tos.Any(t => t.Layer == "Infrastructure" && t.Conditions.SequenceEqual(infrastructure.Conditions))));
    }
}
