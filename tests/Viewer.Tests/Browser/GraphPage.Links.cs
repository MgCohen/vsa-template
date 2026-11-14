using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

public sealed partial class GraphPage
{
    public Task<string[]> Dotted()
    {
        return Page.EvalOnSelectorAllAsync<string[]>("path[marker-end][stroke-dasharray=\"2 4\"]",
            "ps => ps.map(p => p.getAttribute('data-edge'))");
    }

    public async Task<CardPair> FreePair()
    {
        string[] pair = await Page.EvaluateAsync<string[]>(Scripts.FreePair);
        return new CardPair(pair[0], pair[1]);
    }

    public ILocator Port(string node, string side, string row)
    {
        return Page.Locator($"g.card[data-node=\"{node}\"] {row} circle.port-hit[data-side=\"{side}\"]");
    }

    public async Task<int> DrawLink(ILocator from, ILocator to)
    {
        LocatorBoundingBoxResult start = (await from.BoundingBoxAsync())!;
        LocatorBoundingBoxResult end = (await to.BoundingBoxAsync())!;
        float x0 = start.X + start.Width / 2;
        float y0 = start.Y + start.Height / 2;
        float x1 = end.X + end.Width / 2;
        float y1 = end.Y + end.Height / 2;
        await Page.Mouse.MoveAsync(x0, y0);
        await Page.Mouse.DownAsync();
        for (int i = 1; i <= 12; i++)
        {
            await Page.Mouse.MoveAsync(x0 + (x1 - x0) * i / 12, y0 + (y1 - y0) * i / 12);
        }
        int drafts = await Page.EvalOnSelectorAllAsync<int>("path.draft", "ps => ps.length");
        await Page.Mouse.UpAsync();
        return drafts;
    }

    public Task<bool> EndsOnPorts(string id, CardPair pair, string fromRow, string toRow)
    {
        return Page.EvaluateAsync<bool>(Scripts.EndsOnPorts, new object[] { id, pair.From, fromRow, pair.To, toRow });
    }

    public Task<string?> EdgeAttribute(string id, string name)
    {
        return Page.Locator($"path[marker-end][data-edge=\"{id}\"]").GetAttributeAsync(name);
    }

    public async Task<bool> DeleteLink(string id)
    {
        double[] at = await Page.EvalOnSelectorAsync<double[]>($"path.edge-hit[data-edge=\"{id}\"]",
            Scripts.EdgeMiddle);
        await Page.Mouse.ClickAsync((float)at[0], (float)at[1]);
        await Settle();
        bool picked = (await Panel()).StartsWith("Link", StringComparison.Ordinal);
        await Page.Keyboard.PressAsync("Delete");
        return picked;
    }

    public async Task<string> AddWhatIfLink()
    {
        CardPair pair = await FreePair();
        int before = await EdgeCount();
        await DrawLink(Port(pair.From, "Out", "g.row").First, Port(pair.To, "In", "g.row").Last);
        await WaitEdges(before + 1);
        await Settle();
        return (await Dotted()).Single();
    }
}
