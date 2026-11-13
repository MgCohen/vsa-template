using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

public sealed partial class GraphPage
{
    public async Task ShowDefinition()
    {
        await Page.ClickAsync("button:has-text(\"View: Definition\")");
        await WaitFor("() => document.querySelectorAll('g.card.part').length > 40", null);
        await Settle();
    }

    public async Task WaitFor(string script, object? argument)
    {
        await Page.WaitForFunctionAsync(script, argument, new PageWaitForFunctionOptions { Timeout = 20000 });
    }

    public async Task<bool> PickEdge(string id)
    {
        double[] at = await Page.EvalOnSelectorAsync<double[]>($"path.edge-hit[data-edge=\"{id}\"]",
            Scripts.VisibleEdgePoint);
        Assert.That(at[0], Is.Not.Negative, $"No point of the edge {id} is reachable by the mouse.");
        await Page.Mouse.ClickAsync((float)at[0], (float)at[1]);
        await Settle();
        return (await Panel()).StartsWith("Link", StringComparison.Ordinal);
    }

    public async Task<string> FirstReachable(IEnumerable<string> ids)
    {
        foreach (string id in ids)
        {
            double[] at = await Page.EvalOnSelectorAsync<double[]>($"path.edge-hit[data-edge=\"{id}\"]",
                Scripts.VisibleEdgePoint);
            if (at[0] >= 0)
            {
                return id;
            }
        }
        Assert.Fail($"No edge of {string.Join(", ", ids)} is reachable by the mouse.");
        return "";
    }

    public async Task<IReadOnlyList<string>> FirstMerged(IEnumerable<IReadOnlyList<string>> groups)
    {
        List<IReadOnlyList<string>> merged = [];
        foreach (IReadOnlyList<string> group in groups)
        {
            if (await Page.Locator($"path.edge-hit[data-edge=\"{group[1]}\"]").CountAsync() == 0)
            {
                merged.Add(group);
            }
        }
        if (merged.Count == 0)
        {
            Assert.Ignore("The definition has no two rules on one part pair, so no edge is merged.");
        }
        string first = await FirstReachable(merged.Select(g => g[0]));
        return merged.First(g => g[0] == first);
    }

    public Task<string[]> Pending()
    {
        return Page.EvalOnSelectorAllAsync<string[]>("path[marker-end][data-pending=\"true\"]",
            "ps => ps.map(p => p.dataset.edge)");
    }

    public Task<string[]> Labels()
    {
        return Page.EvalOnSelectorAllAsync<string[]>(".edge-label", "ls => ls.map(l => l.textContent)");
    }

    public Task<string[]> RuleButtons()
    {
        return Page.EvalOnSelectorAllAsync<string[]>(".inspect button.remove-rule",
            "bs => bs.map(b => b.dataset.rule + '=' + b.textContent)");
    }

    public async Task<string> Message()
    {
        return await Page.TextContentAsync(".tools .message") ?? "";
    }

    public async Task WaitChangesStartWith(string text)
    {
        await WaitFor(Scripts.ChangesStartWith, text);
        await Settle();
    }

    public async Task RemoveRule(string id)
    {
        await PickEdge(id);
        await Page.Keyboard.PressAsync("Delete");
        await WaitFor(Scripts.EdgeRemoved, id);
    }

    public async Task FlipRule(string id, string access)
    {
        await PickEdge(id);
        await Press("Flip allow / deny");
        await WaitFor(Scripts.EdgeAccess, new object[] { id, access });
    }

    public async Task<int> AddConditionalRule()
    {
        await Page.SelectOptionAsync("select.draft-access", "Allow");
        await Page.SelectOptionAsync("select.draft-condition", "InOwn");
        await Page.SelectOptionAsync("select.draft-layer", "Feature");
        int drafts = await DrawLink(Port("p39", "Out", ""), Port("p38", "In", ""));
        await WaitFor(Scripts.NewRuleDrawn, null);
        await Settle();
        return drafts;
    }
}
