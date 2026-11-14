using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class DragTests : ViewerPageTest
{
    [Test]
    public async Task A_node_drag_moves_the_node_and_its_edges_but_not_the_view()
    {
        await Graph.StartEditing();
        string view = await Graph.Transform();
        (int index, float x, float y) = await Graph.ReachableHead();
        ILocator card = Graph.Page.Locator("svg.canvas g.card").Nth(index);
        string? before = await card.GetAttributeAsync("transform");
        string routes = await Graph.Paths();

        await Graph.Page.Mouse.MoveAsync(x, y);
        await Graph.Page.Mouse.DownAsync();
        for (int i = 1; i <= 5; i++)
        {
            await Graph.Page.Mouse.MoveAsync(x + i * 8, y + i * 4);
        }

        Assert.That(await card.GetAttributeAsync("transform"), Is.Not.EqualTo(before));
        Assert.That(await Graph.Transform(), Is.EqualTo(view));
        Assert.That(await Graph.Paths(), Is.Not.EqualTo(routes));
        await Graph.Page.Mouse.UpAsync();
    }

    [Test]
    public async Task A_drop_gives_new_routes_that_cross_no_card()
    {
        await Graph.StartEditing();

        await DropFirstCard();

        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
    }

    [Test]
    public async Task A_drop_below_a_neighbour_keeps_the_drop_order()
    {
        await Graph.StartEditing();
        await DropFirstCard();
        await Graph.Press("Reset view");
        await Graph.Page.WaitForTimeoutAsync(500);
        IReadOnlyList<CardBox> before = await Graph.Cards();
        string routes = await Graph.Paths();
        (CardBox moved, CardBox below) = before
            .SelectMany(a => before.Where(b => Math.Abs(a.X - b.X) < 5 && b.Y > a.Y && b.Y - a.Y < 150)
                .Select(b => (a, b)))
            .First();
        int index = before.ToList().IndexOf(moved);

        string preview = await DragHeader(index, below.Y - moved.Y + 20);
        await Graph.WaitRoutesChange(preview);
        await Graph.Settle();
        IReadOnlyList<CardBox> after = await Graph.Cards();

        Assert.That(await Graph.Paths(), Is.Not.EqualTo(routes));
        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
        double neighbour = after.Single(c => c.Label == below.Label).Y;
        Assert.That(after.Single(c => c.Label == moved.Label).Y, Is.GreaterThan(neighbour));
    }

    private async Task DropFirstCard()
    {
        (int _, float x, float y) = await Graph.ReachableHead();
        await Graph.Page.Mouse.MoveAsync(x, y);
        await Graph.Page.Mouse.DownAsync();
        for (int i = 1; i <= 5; i++)
        {
            await Graph.Page.Mouse.MoveAsync(x + i * 8, y + i * 4);
        }
        string during = await Graph.Paths();
        await Graph.Page.Mouse.UpAsync();
        await Graph.WaitRoutesChange(during);
        await Graph.Settle();
    }

    private async Task<string> DragHeader(int card, double drop)
    {
        ILocator head = Graph.Page.Locator("g.card").Nth(card).Locator("g.head");
        LocatorBoundingBoxResult box = (await head.BoundingBoxAsync())!;
        await Graph.Page.Mouse.MoveAsync(box.X + 30, box.Y + 10);
        await Graph.Page.Mouse.DownAsync();
        for (int i = 1; i <= 10; i++)
        {
            await Graph.Page.Mouse.MoveAsync(box.X + 30, (float)(box.Y + 10 + i * drop / 10));
        }
        string preview = await Graph.Paths();
        await Graph.Page.Mouse.UpAsync();
        return preview;
    }
}
