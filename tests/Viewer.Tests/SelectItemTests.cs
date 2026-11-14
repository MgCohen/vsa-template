using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Slices.SelectItem;
using VSA.Testing.Unit;

namespace Viewer.Tests;

public sealed class SelectItemTests : VsaUnitTest
{
    private static readonly Edge link = new("e1", "a", null, "b", null, Access.Allow, [], []);
    private static readonly LayoutData layout = new(new Size(0, 0), [], [], [link]);

    [Test]
    public async Task A_node_selects_its_links_and_their_ends()
    {
        SelectItemRequest request = new(layout, new Focus("a", null));
        VSA.Contracts.Result<SelectItemResponse> result = await Handle(request);

        Assert.That(result.Value.Hot, Is.EqualTo(new[] { "e1" }));
        Assert.That(result.Value.Related, Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task The_background_selects_nothing()
    {
        VSA.Contracts.Result<SelectItemResponse> result = await Handle(new SelectItemRequest(layout, Focus.None));

        Assert.That(result.Value.Hot, Is.Empty);
    }
}
