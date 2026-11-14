using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using Microsoft.Playwright;
using Viewer.Tests.Export;

namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class ExportTests : ViewerPageTest
{
    private string allowed = "";
    private string denied = "";

    [Test]
    public async Task Export_gives_a_definition_file_with_the_changes()
    {
        await MakeChanges();

        Definition edited = ArchitectureImport.Read(await ExportedJson());

        Assert.That(edited.Rules, Has.None.Matches<Rule>(r => Covers(r, allowed, Access.Allow)));
        Assert.That(edited.Rules, Has.None.Matches<Rule>(r => Covers(r, denied, Access.Deny)));
        Assert.That(edited.Rules, Has.Some.Matches<Rule>(r => Covers(r, denied, Access.Allow)));
        Assert.That(edited.Rules, Has.Some.Matches<Rule>(r => r.Access == Access.Allow
            && r.Froms.Any(f => f.Layer == "Presenters")
            && r.Tos.Any(t => t.Layer == "Infrastructure" && t.Conditions.Contains(new Condition(Condition.InSame, "Feature")))));
    }

    [Test]
    public async Task Copy_puts_the_definition_file_on_the_clipboard()
    {
        await MakeChanges();
        string json = await ExportedJson();

        await Graph.Press("Copy JSON");
        string copied = await Graph.Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        Assert.That(copied.Replace("\r\n", "\n", StringComparison.Ordinal), Is.EqualTo(json.Replace("\r\n", "\n", StringComparison.Ordinal)));
    }

    [Test]
    public async Task The_download_link_gives_the_architecture_file()
    {
        await MakeChanges();
        await ExportedJson();

        IDownload download = await Graph.Page.RunAndWaitForDownloadAsync(() => Graph.Page.ClickAsync("a.download"));

        Assert.That(download.SuggestedFilename, Is.EqualTo("vsa.architecture.json"));
    }

    private static bool Covers(Rule rule, string edge, string access)
    {
        string from = edge.Split(':')[1].Split('>')[0];
        string to = edge.Split('>')[1];
        return rule.Access == access && rule.Froms.Any(f => f.Layer == from) && rule.Tos.Any(t => t.Layer == to);
    }

    private async Task<string> ExportedJson()
    {
        await Graph.Press("Export definition");
        await Graph.Page.WaitForSelectorAsync(".export pre.json");
        return await Graph.Page.TextContentAsync(".export pre.json") ?? "";
    }

    private async Task MakeChanges()
    {
        await Graph.StartEditing();
        await Graph.ShowDefinition();
        allowed = await Graph.FirstReachable(NotesFacts.AllowedRules);
        denied = await Graph.FirstReachable(NotesFacts.DeniedRules);
        await Graph.RemoveRule(allowed);
        await Graph.FlipRule(denied, "Allow");
        await Graph.AddConditionalRule();
    }
}
