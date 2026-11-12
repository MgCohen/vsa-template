namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class ReviewTests : ViewerPageTest
{
    private const string Merged = "r18:Handlers.Requests>Contracts.Events";
    private const string First = "r18:Handlers.Requests>Contracts.Events";
    private const string Second = "r19:Handlers.Requests>Contracts.Events";

    [Test]
    public async Task A_merged_edge_lists_each_rule_and_Delete_asks_for_a_rule()
    {
        await Graph.ShowDefinition();
        await Graph.StartEditing();

        bool picked = await Graph.PickEdge(Merged);
        string before = await Graph.Changes();
        await Graph.Page.Keyboard.PressAsync("Delete");
        await Graph.Settle();

        Assert.That(picked, Is.True);
        Assert.That(await Graph.RuleButtons(), Is.EqualTo(new[] { First + "=Remove", Second + "=Remove" }));
        Assert.That(await Graph.Page.Locator(".inspect button.flip-rule").CountAsync(), Is.EqualTo(2));
        Assert.That(await Graph.Changes(), Is.EqualTo(before));
        Assert.That(await Graph.Message(), Is.EqualTo("Select a rule in the details panel."));
    }

    [Test]
    public async Task Flip_Remove_and_Restore_in_the_details_panel_act_on_one_rule()
    {
        await Graph.ShowDefinition();
        await Graph.StartEditing();
        await Graph.PickEdge(Merged);

        await Graph.Page.ClickAsync($".inspect button.flip-rule[data-rule=\"{Second}\"]");
        await Graph.WaitChangesStartWith("1 rule");
        string flippedAccess = await Graph.EdgeAttribute(Merged, "data-access") ?? "";
        string panel = await Graph.Panel();
        await Graph.Page.ClickAsync($".inspect button.remove-rule[data-rule=\"{First}\"]");
        await Graph.WaitChangesStartWith("2 rule");
        string[] removed = await Graph.RuleButtons();
        string? edgeRemoved = await Graph.EdgeAttribute(Merged, "data-removed");
        await Graph.Page.ClickAsync($".inspect button.remove-rule[data-rule=\"{First}\"]");
        await Graph.WaitChangesStartWith("1 rule");

        Assert.That(flippedAccess, Is.EqualTo("Deny"));
        Assert.That(panel, Does.Contain(": Deny"));
        Assert.That(removed, Is.EqualTo(new[] { First + "=Restore", Second + "=Remove" }));
        Assert.That(edgeRemoved, Is.EqualTo("false"));
        Assert.That(await Graph.RuleButtons(), Is.EqualTo(new[] { First + "=Remove", Second + "=Remove" }));
    }

    [Test]
    public async Task Reset_view_keeps_the_changes_and_Discard_clears_them()
    {
        await Graph.ShowDefinition();
        await Graph.StartEditing();
        await Graph.RemoveRule("r20:RootClass>Core");
        await Graph.FlipRule("r12:Plugin>Feature", "Allow");
        await Graph.Press("Reset view");
        string fit = await Graph.Transform();
        int[] point = await Graph.Page.EvaluateAsync<int[]>(Scripts.BackgroundPoint);

        await Graph.Drag(point[0], point[1], 5, -20, -10);
        string panned = await Graph.Transform();
        await Graph.Press("Reset view");

        Assert.That(panned, Is.Not.EqualTo(fit));
        Assert.That(await Graph.Transform(), Is.EqualTo(fit));
        Assert.That(await Graph.Changes(), Is.EqualTo("2 rule changes, 0 what-if links"));
        Assert.That(await Graph.EdgeAttribute("r20:RootClass>Core", "data-removed"), Is.EqualTo("true"));
        await Graph.Page.ClickAsync("button:has-text(\"Discard changes\")");
        await Graph.WaitChangesStartWith("0 rule");
        Assert.That(await Graph.Pending(), Is.Empty);
        Assert.That(await Graph.EdgeAttribute("r20:RootClass>Core", "data-removed"), Is.EqualTo("false"));
        Assert.That(await Graph.EdgeAttribute("r12:Plugin>Feature", "data-access"), Is.EqualTo("Deny"));
    }
}
