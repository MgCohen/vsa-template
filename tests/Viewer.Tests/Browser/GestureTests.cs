using Microsoft.Playwright;

namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class GestureTests : ViewerPageTest
{
    private const string NoSelection = "Select a row";

    [Test]
    public async Task The_fields_shape_places_the_rows_from_the_shape_data()
    {
        string rows = await Graph.Page.EvalOnSelectorAsync<string>("g.card", Scripts.RowPlaces);

        Assert.That(rows, Does.StartWith("34/28 62/28"));
    }

    [Test]
    public async Task A_drag_on_the_background_pans_the_view()
    {
        string before = await Graph.Transform();

        await Graph.Drag(1000, 1150, 10, 10, -5);

        Assert.That(await Graph.Transform(), Is.Not.EqualTo(before));
    }

    [Test]
    public async Task The_wheel_zooms_in_at_the_cursor_and_back_out()
    {
        string before = await Graph.Transform();
        await Graph.Page.Mouse.MoveAsync(1000, 600);

        await Graph.Page.Mouse.WheelAsync(0, -100);
        await Graph.Settle();
        string zoomed = await Graph.Transform();
        await Graph.Page.Mouse.WheelAsync(0, 100);
        await Graph.Settle();

        Assert.That(GraphPage.Scale(zoomed), Is.GreaterThan(GraphPage.Scale(before)));
        Assert.That(GraphPage.Scale(await Graph.Transform()), Is.EqualTo(GraphPage.Scale(before)).Within(1e-6));
    }

    [Test]
    public async Task A_row_click_selects_the_row()
    {
        ILocator row = Graph.Page.Locator("g.row", new PageLocatorOptions { HasText = "NoteItem" }).First;

        await row.ClickAsync();
        await Graph.Settle();

        Assert.That(await Graph.Panel(), Does.Contain("NoteItem"));
        Assert.That(await row.Locator("rect").First.GetAttributeAsync("fill"), Is.EqualTo("#eaf1ff"));
    }

    [Test]
    public async Task A_pan_keeps_the_selection_and_a_background_click_clears_it()
    {
        await Graph.Page.Locator("g.row", new PageLocatorOptions { HasText = "NoteItem" }).First.ClickAsync();
        await Graph.Settle();

        await Graph.Drag(1000, 1150, 5, 10, 0);
        string afterPan = await Graph.Panel();
        int y = await Graph.Page.EvaluateAsync<int>(Scripts.BackgroundY);
        await Graph.Page.Mouse.ClickAsync(1000, y);
        await Graph.Settle();

        Assert.That(afterPan, Does.Contain("NoteItem"));
        Assert.That(await Graph.Panel(), Does.StartWith(NoSelection));
    }

    [Test]
    public async Task A_header_click_selects_the_node_and_a_control_click_clears_it()
    {
        ILocator head = Graph.Page.Locator("g.head").First;

        await head.ClickAsync();
        await Graph.Settle();
        string selected = await Graph.Panel();
        await head.ClickAsync(new LocatorClickOptions { Modifiers = [KeyboardModifier.Control] });
        await Graph.Settle();

        Assert.That(selected, Does.Not.StartWith(NoSelection));
        Assert.That(await Graph.Panel(), Does.StartWith(NoSelection));
    }
}
