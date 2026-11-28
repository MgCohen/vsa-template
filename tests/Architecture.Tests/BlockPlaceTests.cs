namespace EngineCore.Tests;

using Facts.Infrastructure;
using Umpire.Architecture;

public record Tool : Concept {
    public override string Folder => "Tools";
}

public record PackPlace : Concept {
    public override string Folder => "Packs";
}

public sealed record ToolKit : Umpire.Architecture.Blocks.Block {
    public override string Folder => "Kit";

    protected override void Define(Part part) => part.Add<Item>();
}

public sealed class PlaceArchitecture : Architecture {
    protected override void Define(Part block, Part kernel) {
        var box = block.AddGroup<Box>();
        box.Add<Item>().InParentFolder();
        box.Add<Tag>();
        block.AddBlock<Tool>();
        block.AddBlockGroup<PackPlace>();
    }
}

public sealed class BlockPlaceTests {
    string block = "";

    [SetUp]
    public void CreateBlock() => block = Directory.CreateTempSubdirectory("umpire-places-").FullName;

    [TearDown]
    public void DeleteBlock() => Directory.Delete(block, recursive: true);

    [Test]
    public void A_type_in_an_inner_block_binds_to_its_place_and_not_to_the_outer_parts() {
        var bindings = Place([Fact("Tools", "Note")], Inner("Tools"));

        Assert.That(bindings.All.Single().Part.Is(typeof(Tool)), Is.True);
        Assert.That(bindings.All.Single().Part.IsBlockPlace, Is.True);
        Assert.That(bindings.Unmapped, Is.Empty);
    }

    [Test]
    public void A_type_in_a_block_of_a_block_group_binds_to_the_group_with_its_folder() {
        var bound = Place([Fact("Packs/Alpha/Kernel", "Note")], Inner("Packs/Alpha")).All.Single();

        Assert.That(bound.Part.Is(typeof(PackPlace)), Is.True);
        Assert.That(bound.Values["PackPlace"], Is.EqualTo("Alpha"));
    }

    [Test]
    public void A_type_in_an_inner_block_with_no_place_is_not_checked() {
        var bindings = Place([Fact("Other", "Note")], Inner("Other"));

        Assert.That(bindings.All, Is.Empty);
        Assert.That(bindings.Unmapped, Is.Empty);
    }

    [Test]
    public void A_type_in_a_folder_with_no_part_and_no_block_is_unmapped() {
        var bindings = Place([Fact("Other", "Note")], []);

        Assert.That(bindings.Unmapped, Has.Count.EqualTo(1));
    }

    [Test]
    public void An_outer_part_that_covers_an_inner_block_is_a_problem() {
        var covered = Resolved().CoveredBlocks(block, Inner("Boxes/a"));

        Assert.That(covered.Select(c => c.Part.Name), Is.EqualTo(new[] { "Box" }));
        Assert.That(covered.Single().Folder, Is.EqualTo(Path.Combine(block, "Boxes", "a")));
    }

    [Test]
    public void An_outer_part_that_contains_an_inner_block_is_a_problem() {
        var covered = Resolved().CoveredBlocks(block, Inner("Boxes/a/Deep/Store"));

        Assert.That(covered.Select(c => c.Part.Name), Is.EqualTo(new[] { "Box" }));
    }

    [Test]
    public void A_block_above_the_part_folders_names_the_nearest_part_below_it() {
        var covered = Resolved().CoveredBlocks(block, Inner("Boxes"));

        Assert.That(covered.Select(c => c.Part.Name), Is.EqualTo(new[] { "Box" }));
    }

    [Test]
    public void A_block_in_a_part_folder_names_that_part_and_not_a_part_below_it() {
        var covered = Resolved().CoveredBlocks(block, Inner("Boxes/a"));

        Assert.That(covered.Select(c => c.Part.Name), Is.EqualTo(new[] { "Box" }));
    }

    [Test]
    public void A_block_in_an_inner_block_is_the_problem_of_that_block() {
        Assert.That(Resolved().CoveredBlocks(block, Inner("Tools", "Tools/Boxes/a")), Is.Empty);
    }

    [Test]
    public void A_block_out_of_each_part_folder_is_not_covered() {
        Assert.That(Resolved().CoveredBlocks(block, Inner("Other/Store")), Is.Empty);
    }

    [Test]
    public void A_block_at_its_place_is_not_covered() {
        Directory.CreateDirectory(Path.Combine(block, "Packs", "Alpha"));

        Assert.That(Resolved().CoveredBlocks(block, Inner("Tools", "Packs/Alpha")), Is.Empty);
    }

    [Test]
    public void A_place_with_no_block_is_a_problem() {
        Directory.CreateDirectory(Path.Combine(block, "Tools"));
        var empty = Empty(Resolved(), []);

        Assert.That(empty.Select(e => e.Folder), Is.EqualTo(new[] { Path.Combine(block, "Tools") }));
    }

    [Test]
    public void A_place_with_no_folder_and_no_body_is_a_problem() {
        var empty = Empty(Resolved(), []);

        Assert.That(empty.Select(e => e.Folder), Is.EqualTo(new[] { Path.Combine(block, "Tools") }));
    }

    [Test]
    public void A_place_of_a_block_with_a_body_and_no_folder_is_the_DLL_form_and_no_problem() {
        Assert.That(Empty(new ResolvedArchitecture(new BodyPlaceArchitecture()), []), Is.Empty);
    }

    [Test]
    public void Each_subfolder_of_a_block_group_must_hold_a_block() {
        foreach (var folder in new[] { "Alpha", "Beta", "bin", "obj", "artifacts~" }) {
            Directory.CreateDirectory(Path.Combine(block, "Packs", folder));
        }

        var empty = Empty(Resolved(), Inner("Tools", "Packs/Alpha"));

        Assert.That(empty.Select(e => e.Folder), Is.EqualTo(new[] { Path.Combine(block, "Packs", "Beta") }));
    }

    [Test]
    public void A_block_place_takes_no_parts() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new PlaceWithChild()));

        Assert.That(error!.Message, Does.Contain("Tool is the place of another block"));
    }

    static ResolvedArchitecture Resolved() => new(new PlaceArchitecture());

    Bindings Place(TypeFacts[] types, IReadOnlyList<string> inner) => Resolved().Place(types, block, inner);

    TypeFacts Fact(string folder, string name) =>
        new($"App.{name}", name, "App", new Source(Path.Combine(block, folder, name + ".cs"), 1), [], [], [], []);

    IReadOnlyList<BlockPlace> Empty(ResolvedArchitecture resolved, IReadOnlyList<string> inner) =>
        resolved.EmptyPlaces(block, inner, new BlockFolderSource().Read(new BlockFolderQuery(block, resolved.BlockPlaceFolders())));

    IReadOnlyList<string> Inner(params string[] folders) => [.. folders.Select(f => Path.Combine(block, f))];

    sealed class PlaceWithChild : Architecture {
        protected override void Define(Part block, Part kernel) => block.AddBlock<Tool>().Add<Item>();
    }

    sealed class BodyPlaceArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) => block.AddBlock<ToolKit>();
    }
}
