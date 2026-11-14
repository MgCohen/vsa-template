using VSA.Architecture;

namespace Architecture.Tests;

public record GroupRoot : Anchor;

public sealed class AnchorTests {
    [Test]
    public void An_anchor_is_in_the_folder_of_its_parent() {
        Part? anchor = null;
        _ = new InlineDefinition(root => anchor = root.AddGroup<Group>().Add<GroupRoot>()).Root;

        Assert.That(anchor!.HasFolder, Is.False);
    }

    [Test]
    public void An_anchor_has_the_path_of_its_group_instance() {
        Part? anchor = null;
        Part? group = null;
        _ = new InlineDefinition(root => {
            group = root.AddGroup<Group>();
            anchor = group.Add<GroupRoot>();
        }).Root;

        Assert.That(anchor!.Path, Is.EqualTo(group!.Path));
    }

    [Test]
    public void A_concept_that_is_not_an_anchor_has_its_own_folder() {
        Part? child = null;
        _ = new InlineDefinition(root => child = root.AddGroup<Group>().Add<Child>()).Root;

        Assert.That(child!.HasFolder, Is.True);
    }
}
