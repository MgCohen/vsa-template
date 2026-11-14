using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

public sealed partial class GraphPage
{
    private const float LongWait = 15000;

    public GraphPage(IPage page)
    {
        Page = page;
    }

    public IPage Page { get; }

    public static double Scale(string transform)
    {
        string scale = transform.Split("scale(")[1];
        return double.Parse(scale[..scale.IndexOf(')', StringComparison.Ordinal)], CultureInfo.InvariantCulture);
    }

    public Task Settle()
    {
        return Page.WaitForTimeoutAsync(400);
    }

    public async Task Press(string button)
    {
        await Page.ClickAsync($"button:has-text(\"{button}\")");
        await Settle();
    }

    public Task<string> Transform()
    {
        return Page.EvalOnSelectorAsync<string>("svg.canvas > g", "g => g.getAttribute('transform')");
    }

    public Task<string> Panel()
    {
        return Page.EvalOnSelectorAsync<string>(".inspect", "d => d.innerText");
    }

    public Task<string> Changes()
    {
        return Page.EvalOnSelectorAsync<string>(".toolbar .changes", "s => s.textContent.trim()");
    }

    public Task<string> Paths()
    {
        return Page.EvalOnSelectorAllAsync<string>("path[marker-end]",
            "ps => ps.map(p => p.getAttribute('d')).join('|')");
    }

    public Task<int> EdgeCount()
    {
        return Page.EvalOnSelectorAllAsync<int>("path[marker-end]", "ps => ps.length");
    }

    public Task<int> EdgesThroughCards()
    {
        return Page.EvaluateAsync<int>(Scripts.EdgesThroughCards);
    }

    public async Task<IReadOnlyList<CardBox>> Cards()
    {
        JsonElement cards = await Page.EvaluateAsync<JsonElement>(Scripts.Cards);
        return [.. cards.EnumerateArray()
            .Select(c => new CardBox(c[0].GetString()!, c[1].GetDouble(), c[2].GetDouble()))];
    }

    public async Task<(int Index, float X, float Y)> ReachableHead()
    {
        JsonElement head = await Page.EvaluateAsync<JsonElement>(Scripts.ReachableHead);
        int index = head[0].GetInt32();
        Assert.That(index, Is.Not.Negative, "No card header is reachable by the mouse.");
        return (index, (float)head[1].GetDouble(), (float)head[2].GetDouble());
    }

    public async Task WaitRoutesChange(string before)
    {
        await Page.WaitForFunctionAsync(Scripts.RoutesChanged, before,
            new PageWaitForFunctionOptions { Timeout = LongWait });
    }

    public async Task WaitEdges(int count)
    {
        await Page.WaitForFunctionAsync("n => document.querySelectorAll('path[marker-end]').length === n", count,
            new PageWaitForFunctionOptions { Timeout = LongWait });
    }

    public async Task Drag(double x, double y, int steps, double stepX, double stepY)
    {
        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        for (int i = 1; i <= steps; i++)
        {
            await Page.Mouse.MoveAsync((float)(x + i * stepX), (float)(y + i * stepY));
        }
        await Page.Mouse.UpAsync();
        await Settle();
    }

    public async Task UseShape(string shape)
    {
        string before = await Paths();
        await Page.ClickAsync($"button:has-text(\"Shape: {shape}\")");
        await WaitRoutesChange(before);
        await Settle();
    }

    public async Task StartEditing()
    {
        string? state = await Page.GetAttributeAsync("button:has-text(\"Edit: drag nodes\")", "class");
        if (state?.Contains("on", StringComparison.Ordinal) != true)
        {
            await Press("Edit: drag nodes");
        }
    }
}
