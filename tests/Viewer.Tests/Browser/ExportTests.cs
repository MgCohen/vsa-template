using Viewer.Tests.Export;

using System.Text.Json;
using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class ExportTests : ViewerPageTest
{
    private string[] expectedCode = [];

    [Test]
    public async Task Export_gives_typed_operations_and_the_CSharp_lines()
    {
        await MakeChanges();

        await Graph.Press("Export changes");
        await Graph.Page.WaitForSelectorAsync(".export pre.code");

        using JsonDocument json = JsonDocument.Parse(await Graph.Page.TextContentAsync(".export pre.json") ?? "");
        JsonElement changes = json.RootElement.GetProperty("changes");
        JsonElement add = changes[2];
        JsonElement[] whatIf = [.. json.RootElement.GetProperty("whatIf").EnumerateArray()];
        Assert.That(changes.EnumerateArray().Select(c => c.GetProperty("op").GetString()),
            Is.EqualTo(new[] { "RemoveRule", "ChangeAccess", "AddRule" }));
        Assert.That(add.GetProperty("from").GetString(), Is.EqualTo("Presenters"));
        Assert.That(add.GetProperty("to").GetString(), Is.EqualTo("Infrastructure"));
        Assert.That(add.GetProperty("access").GetString(), Is.EqualTo("Uses"));
        Assert.That(add.GetProperty("condition").GetProperty("kind").GetString(), Is.EqualTo("InOwn"));
        Assert.That(add.GetProperty("condition").GetProperty("layer").GetString(), Is.EqualTo("Feature"));
        Assert.That(whatIf.Select(w => w.GetProperty("op").GetString()), Is.EqualTo(new[] { "AddLink" }));
        Assert.That(await Code(), Is.EqualTo(expectedCode));
    }

    [Test]
    public async Task Copy_puts_the_CSharp_lines_on_the_clipboard()
    {
        await MakeChanges();
        await Graph.Press("Export changes");
        await Graph.Page.WaitForSelectorAsync(".export pre.code");

        await Graph.Press("Copy C#");
        string copied = await Graph.Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        string lines = copied.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.That(lines, Is.EqualTo(string.Join('\n', expectedCode)));
    }

    [Test]
    public async Task The_download_link_gives_changes_json()
    {
        await MakeChanges();
        await Graph.Press("Export changes");
        await Graph.Page.WaitForSelectorAsync(".export pre.code");

        IDownload download = await Graph.Page.RunAndWaitForDownloadAsync(() => Graph.Page.ClickAsync("a.download"));

        Assert.That(download.SuggestedFilename, Is.EqualTo("changes.json"));
    }

    private static string From(string edge)
    {
        return edge.Split(':')[1].Split('>')[0];
    }

    private static string To(string edge)
    {
        return edge.Split('>')[1];
    }

    private async Task<string[]> Code()
    {
        return (await Graph.Page.TextContentAsync(".export pre.code") ?? "").Split('\n');
    }

    private async Task MakeChanges()
    {
        await Graph.StartEditing();
        await Graph.AddWhatIfLink();
        await Graph.ShowDefinition();
        string allowed = await Graph.FirstReachable(NotesFacts.AllowedRules);
        string denied = await Graph.FirstReachable(NotesFacts.DeniedRules);
        expectedCode =
        [
            $"- map.For<{From(allowed)}>().Uses<{To(allowed)}>();",
            $"- map.For<{From(denied)}>().Denies<{To(denied)}>();",
            $"+ map.For<{From(denied)}>().Uses<{To(denied)}>();",
            "+ map.For<Presenters>().Uses<Infrastructure>().InOwn<Feature>();",
        ];
        await Graph.RemoveRule(allowed);
        await Graph.FlipRule(denied, "Allow");
        await Graph.AddConditionalRule();
    }
}
