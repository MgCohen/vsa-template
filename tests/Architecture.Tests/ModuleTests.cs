using VSA.Architecture;

namespace EngineCore.Tests;

public record GroupRoot : Module;

public sealed class ModuleTests {
    [Test]
    public void A_module_is_in_the_folder_of_its_parent() {
        var root = new InlineArchitecture(root => root.AddGroup<Group>().Add<GroupRoot>()).Root;

        Assert.That(PartOf<GroupRoot>(root).HasFolder, Is.False);
    }

    [Test]
    public void A_module_has_the_path_of_its_group_instance() {
        var root = new InlineArchitecture(root => root.AddGroup<Group>().Add<GroupRoot>()).Root;

        Assert.That(PartOf<GroupRoot>(root).Path, Is.EqualTo(PartOf<Group>(root).Path));
    }

    [Test]
    public void A_concept_that_is_not_a_module_has_its_own_folder() {
        var root = new InlineArchitecture(root => root.AddGroup<Group>().Add<Child>()).Root;

        Assert.That(PartOf<Child>(root).HasFolder, Is.True);
    }

    static Part PartOf<T>(Root root) where T : Concept => root.Walk().Single(p => p.Is(typeof(T)));
}
