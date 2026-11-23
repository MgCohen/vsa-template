namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record Crate : Block {
    protected override void Define(Part part) {
        part.Add<Child>();
    }
}

public record LargeCrate : Crate {
    protected override void Define(Part part) {
        part.Add<Target>();
    }
}

public record LoopCrate : Block {
    protected override void Define(Part part) {
        part.AddBlock<LoopCrate>();
    }
}

public record OuterCrate : Block {
    protected override void Define(Part part) {
        part.AddBlock<InnerCrate>();
    }
}

public record InnerCrate : Block {
    protected override void Define(Part part) {
        part.AddBlock<OuterCrate>();
    }
}

public sealed class BlockBodyTests {
    [Test]
    public void A_block_place_gets_the_body_of_its_record() {
        var parts = Parts(root => root.AddBlock<Crate>());

        Assert.That(parts, Is.EqualTo(new[] { "Crate", "Child" }));
    }

    [Test]
    public void A_derived_block_record_gets_the_body_of_its_base_and_its_own() {
        var parts = Parts(root => root.AddBlock<LargeCrate>());

        Assert.That(parts, Is.EqualTo(new[] { "LargeCrate", "Child", "Target" }));
    }

    [Test]
    public void The_outer_definition_cannot_add_a_part_to_a_block_place_directly() {
        Assert.Throws<InvalidOperationException>(() => Parts(root => root.AddBlock<Crate>().Add<User>()));
    }

    [Test]
    public void An_overlay_adds_a_part_to_each_placed_block_of_a_kind() {
        var parts = Parts(root => {
            root.AddBlock<Crate>();
            root.Add<Group>().AddBlock<Crate>();
            root.For<Crate>().Add<User>();
        });

        Assert.That(parts, Is.EqualTo(new[] { "Crate", "Child", "User", "Group", "Crate", "Child", "User" }));
    }

    [Test]
    public void An_overlay_of_a_base_kind_covers_a_derived_kind() {
        var parts = Parts(root => {
            root.AddBlock<LargeCrate>();
            root.For<Crate>().Add<User>();
        });

        Assert.That(parts, Does.Contain("User"));
    }

    [Test]
    public void An_overlay_with_no_placed_block_is_an_error() {
        var error = Assert.Throws<InvalidOperationException>(() => Parts(root => root.For<Crate>().Add<User>()))!;

        Assert.That(error.Message, Does.Contain("For<Crate>() finds no placed Crate"));
    }

    [Test]
    public void A_block_that_places_itself_is_an_error() {
        var error = Assert.Throws<InvalidOperationException>(() => Parts(root => root.AddBlock<LoopCrate>()))!;

        Assert.That(error.Message, Does.Contain("the block places itself"));
    }

    [Test]
    public void A_block_that_places_itself_through_another_block_is_an_error() {
        Assert.Throws<InvalidOperationException>(() => Parts(root => root.AddBlock<OuterCrate>()));
    }

    static IReadOnlyList<string> Parts(Action<Part> define) =>
        [.. new ResolvedArchitecture(new InlineArchitecture(define)).Parts.Select(p => p.Name)];
}
