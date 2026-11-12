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
        await Page.Mouse.ClickAsync((float)at[0], (float)at[1]);
        await Settle();
        return (await Panel()).StartsWith("Link", StringComparison.Ordinal);
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
        int drafts = await DrawLink(Port("p36", "Out", ""), Port("p35", "In", ""));
        await WaitFor(Scripts.NewRuleDrawn, null);
        await Settle();
        return drafts;
    }
}
