namespace Viewer.Tests.Browser;

[Category("Browser")]
public sealed class LinkTests : ViewerPageTest
{
    private const int FieldEdges = 39;
    private const int SimpleEdges = 18;

    [Test]
    public async Task The_fields_shape_draws_each_row_edge_and_no_edge_crosses_a_card()
    {
        Assert.That(await Graph.EdgeCount(), Is.EqualTo(FieldEdges));
        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
    }

    [Test]
    public async Task The_simple_shape_resizes_the_cards_and_merges_the_row_edges()
    {
        await Graph.UseShape("Simple");

        string heights = await Graph.Page.EvalOnSelectorAllAsync<string>("g.card > rect:first-child",
            "rs => [...new Set(rs.map(r => r.getAttribute('height')))].join(',')");
        Assert.That(heights, Is.EqualTo("58"));
        Assert.That(await Graph.EdgeCount(), Is.EqualTo(SimpleEdges));
        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
    }

    [Test]
    public async Task A_port_drag_in_the_simple_shape_adds_an_unchecked_link_and_Delete_removes_it()
    {
        await Graph.StartEditing();
        await Graph.UseShape("Simple");
        CardPair pair = await Graph.FreePair();

        int drafts = await Graph.DrawLink(Graph.Port(pair.From, "Out", ""), Graph.Port(pair.To, "In", ""));
        await Graph.WaitEdges(SimpleEdges + 1);
        await Graph.Settle();
        string added = (await Graph.Dotted()).Single();

        Assert.That(drafts, Is.EqualTo(1));
        Assert.That(await Graph.EdgeAttribute(added, "stroke"), Is.EqualTo("#8b93a5"));
        Assert.That(await Graph.EndsOnPorts(added, pair, "", ""), Is.True);
        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
        Assert.That(await Graph.DeleteLink(added), Is.True);
        await Graph.WaitEdges(SimpleEdges);
        await Graph.Settle();
        Assert.That(await Graph.Dotted(), Is.Empty);
        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
    }

    [Test]
    public async Task A_row_port_drag_in_the_fields_shape_adds_a_link_and_Delete_removes_it()
    {
        await Graph.StartEditing();
        CardPair pair = await Graph.FreePair();

        int drafts = await Graph.DrawLink(Graph.Port(pair.From, "Out", "g.row").First,
            Graph.Port(pair.To, "In", "g.row").Last);
        await Graph.WaitEdges(FieldEdges + 1);
        await Graph.Settle();
        string added = (await Graph.Dotted()).Single();

        Assert.That(drafts, Is.EqualTo(1));
        Assert.That(await Graph.EndsOnPorts(added, pair, "g.row", "g.row:last-of-type"), Is.True);
        Assert.That(await Graph.EdgesThroughCards(), Is.Zero);
        Assert.That(await Graph.DeleteLink(added), Is.True);
        await Graph.WaitEdges(FieldEdges);
        await Graph.Settle();
        Assert.That(await Graph.Dotted(), Is.Empty);
        Assert.That(await Graph.Changes(), Is.EqualTo("0 rule changes, 2 what-if links"));
    }
}
