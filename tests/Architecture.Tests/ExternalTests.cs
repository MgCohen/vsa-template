using VSA.Architecture;

namespace Architecture.Tests;

public record Lib : External {
    public override string Assembly => "Lib";
}

public record OtherLib : External {
    public override string Assembly => "OtherLib";
}

public record Group : Layer;

public record Child : Layer;

public sealed class InlineDefinition(Action<Root> define) : ArchitectureDefinition {
    protected override void Define(Root root) {
        define(root);
    }
}

public sealed class ExternalTests {
    [Test]
    public void A_part_under_an_external_gets_its_DLL_name_from_the_parent() {
        Part? child = null;
        _ = new InlineDefinition(root => child = root.Add<Lib>().Add<Child>()).Root;

        Assert.That((child!.Assembly, child.IsExternal), Is.EqualTo(("Lib.Child", true)));
    }

    [Test]
    public void AddMany_of_an_external_throws() {
        Blocks(root => root.AddMany<Lib>(), "AddMany<Lib>() makes a group of an external");
    }

    [Test]
    public void An_external_under_a_group_throws() {
        Blocks(root => root.AddMany<Group>().Add<Lib>(), "the external Lib is under the group Group");
    }

    [Test]
    public void An_external_under_an_external_throws() {
        Blocks(root => root.Add<Lib>().Add<OtherLib>(), "the external OtherLib is under the external Lib");
    }

    [Test]
    public void AddMany_under_an_external_throws() {
        Blocks(root => root.Add<Lib>().AddMany<Child>(), "AddMany<Child>() is under the external Lib");
    }

    [Test]
    public void NoFolder_on_an_external_part_throws() {
        Blocks(root => root.Add<Lib>().NoFolder(), "An external part has no NoFolder");
    }

    static void Blocks(Action<Root> define, string message) {
        var definition = new InlineDefinition(define);

        Assert.That(() => definition.Root, Throws.InvalidOperationException.With.Message.Contains(message));
    }
}
