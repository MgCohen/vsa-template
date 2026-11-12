namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class DefinitionTests : ViewerPageTest
{
    private const string RootToCore = "r20:RootClass>Core";
    private const string PluginToFeature = "r12:Plugin>Feature";

    [Test]
    public async Task The_definition_view_draws_each_part_and_one_edge_for_each_rule_and_layer_pair()
    {
        await Graph.ShowDefinition();

        string[] access = await Graph.Page.EvalOnSelectorAllAsync<string[]>("path[marker-end]",
            "ps => ps.map(p => p.dataset.access)");
        string rows = await Graph.Page.EvalOnSelectorAsync<string>("g.card.part:has(g.part-row)", Scripts.PartRows);
        Assert.That(await Graph.Page.Locator("g.card.part").CountAsync(), Is.EqualTo(43));
        Assert.That(access, Has.Length.EqualTo(37));
        Assert.That(access, Does.Contain("Allow").And.Contain("Conditional").And.Contain("Deny"));
        Assert.That(await Graph.Labels(), Does.Contain("Under<App>"));
        Assert.That(rows, Does.Match(@"^57( 78)*( 99)? h=\d+"));
    }

    [Test]
    public async Task Delete_keeps_a_removed_access_drawn_faded_dashed_and_labeled()
    {
        await Graph.ShowDefinition();
        await Graph.StartEditing();

        await Graph.RemoveRule(RootToCore);

        string look = await Graph.Page.EvalOnSelectorAsync<string>(
            $"path[marker-end][data-edge=\"{RootToCore}\"]", Scripts.RemovedLook);
        Assert.That(look, Is.EqualTo("0.3|4 4|removed|line-through"));
        Assert.That(await Graph.Changes(), Is.EqualTo("1 rule changes, 0 what-if links"));
    }

    [Test]
    public async Task Flip_changes_a_deny_to_a_pending_allow()
    {
        await Graph.ShowDefinition();
        await Graph.StartEditing();

        await Graph.FlipRule(PluginToFeature, "Allow");

        Assert.That(await Graph.EdgeAttribute(PluginToFeature, "data-pending"), Is.EqualTo("true"));
    }

    [Test]
    public async Task A_part_to_part_drag_adds_a_pending_conditional_rule()
    {
        await Graph.ShowDefinition();
        await Graph.StartEditing();

        int drafts = await Graph.AddConditionalRule();

        string added = (await Graph.Pending()).Single(id => id.StartsWith("new:", StringComparison.Ordinal));
        Assert.That(drafts, Is.EqualTo(1));
        Assert.That(await Graph.EdgeAttribute(added, "data-access"), Is.EqualTo("Conditional"));
        Assert.That(await Graph.Labels(), Does.Contain("InOwn<Feature> pending"));
        Assert.That(await Graph.Changes(), Is.EqualTo("1 rule changes, 0 what-if links"));
    }
}
