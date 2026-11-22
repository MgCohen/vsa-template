namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record GroupRoot : Module;

public sealed class ModuleTests {
    [Test]
    public void A_module_is_in_the_folder_of_its_parent() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Group>().Add<GroupRoot>())).Root;

        Assert.That(PartOf<GroupRoot>(block).HasFolder, Is.False);
    }

    [Test]
    public void A_module_has_the_path_of_its_group_instance() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Group>().Add<GroupRoot>())).Root;

        Assert.That(PartOf<GroupRoot>(block).Path, Is.EqualTo(PartOf<Group>(block).Path));
    }

    [Test]
    public void A_concept_that_is_not_a_module_has_its_own_folder() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Group>().Add<Child>())).Root;

        Assert.That(PartOf<Child>(block).HasFolder, Is.True);
    }

    static Part PartOf<T>(Part block) where T : Concept => block.Walk().Single(p => p.Is(typeof(T)));
}
