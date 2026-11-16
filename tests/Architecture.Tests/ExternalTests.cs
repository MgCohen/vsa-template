namespace EngineCore.Tests;

using Umpire.Architecture;

public record Lib : External {
    public override string Assembly => "Lib";
}

public record OtherLib : External {
    public override string Assembly => "OtherLib";
}

public record Group : Concept;

public record Child : Concept;

public sealed class InlineArchitecture(Action<Block> define) : Architecture {
    protected override void Define(Block block) {
        define(block);
    }
}

public sealed class ExternalTests {
    [Test]
    public void A_part_under_an_external_gets_its_DLL_name_from_the_parent() {
        Part? child = null;
        _ = new InlineArchitecture(block => child = block.Add<Lib>().Add<Child>()).Block;

        Assert.That((child!.Assembly, child.IsExternal), Is.EqualTo(("Lib.Child", true)));
    }

    [Test]
    public void A_part_under_an_external_can_name_its_own_DLL() {
        Part? child = null;
        Part? grandchild = null;
        _ = new InlineArchitecture(block => {
            child = block.Add<Lib>().Add<Child>().InAssembly("Other.Name");
            grandchild = child.Add<Group>();
        }).Block;

        Assert.That((child!.Assembly, grandchild!.Assembly), Is.EqualTo(("Other.Name", "Other.Name.Group")));
    }

    [Test]
    public void InAssembly_on_a_part_that_is_not_external_throws() {
        Blocks(block => block.Add<Child>().InAssembly("Other.Name"), "Define: Child is not in an external, so it has no DLL.");
    }

    [Test]
    public void AddMany_of_an_external_throws() {
        Blocks(block => block.AddGroup<Lib>(), "Define: AddGroup<Lib>() makes a group of an external.");
    }

    [Test]
    public void An_external_under_a_group_throws() {
        Blocks(block => block.AddGroup<Group>().Add<Lib>(), "Define: Add<Lib>() is under the group Group.");
    }

    [Test]
    public void An_external_under_an_external_throws() {
        Blocks(block => block.Add<Lib>().Add<OtherLib>(), "Define: Add<OtherLib>() is under the external Lib.");
    }

    [Test]
    public void AddMany_under_an_external_throws() {
        Blocks(block => block.Add<Lib>().AddGroup<Child>(), "Define: AddGroup<Child>() is under the external Lib.");
    }

    [Test]
    public void NoFolder_on_an_external_part_throws() {
        Blocks(block => block.Add<Lib>().InParentFolder(), "An external part has no InParentFolder");
    }

    static void Blocks(Action<Block> define, string message) {
        var definition = new InlineArchitecture(define);

        Assert.That(() => definition.Block, Throws.InvalidOperationException.With.Message.Contains(message));
    }
}
