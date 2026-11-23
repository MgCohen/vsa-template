namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class ModuleTests {
    [Test]
    public void A_concept_in_a_group_has_its_own_folder() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Group>().Add<Child>())).Root;

        Assert.That(PartOf<Child>(block).HasFolder, Is.True);
    }

    static Part PartOf<T>(Part block) where T : Concept => block.Walk().Single(p => p.Is(typeof(T)));
}
