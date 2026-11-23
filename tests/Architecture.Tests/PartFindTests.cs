namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public sealed class PartFindTests {
    [Test]
    public void A_find_adds_a_part_to_each_found_plain_concept() {
        var paths = Paths(root => {
            root.AddGroup<Shelf>().Add<Drawer>();
            root.For<Drawer>().Add<Label>();
        });

        Assert.That(paths, Does.Contain("Shelves/{partfindtests.shelf}/Drawer/Label"));
    }

    [Test]
    public void Under_narrows_the_find_to_the_parts_inside_a_concept() {
        var paths = Paths(root => {
            root.AddGroup<Shelf>().Add<Drawer>();
            root.Add<Desk>().Add<Drawer>();
            root.For<Drawer>().Under<Desk>().Add<Label>();
        });

        Assert.Multiple(() => {
            Assert.That(paths, Does.Contain("Desk/Drawer/Label"));
            Assert.That(paths, Does.Not.Contain("Shelves/{partfindtests.shelf}/Drawer/Label"));
        });
    }

    [Test]
    public void A_find_with_no_match_is_an_error() {
        var error = Assert.Throws<InvalidOperationException>(
            () => Paths(root => root.For<Drawer>().Under<Desk>().Add<Label>()))!;

        Assert.That(error.Message,
            Does.Contain("For<PartFindTests.Drawer>().Under<PartFindTests.Desk>() finds no placed"));
    }

    [Test]
    public void ExtendedBy_stacks_the_body_of_a_block_on_each_found_part() {
        var paths = Paths(root => {
            root.AddGroup<Shelf>().Add<Drawer>();
            root.For<Shelf>().ExtendedBy<Audited>();
        });

        Assert.That(paths, Does.Contain("Shelves/{partfindtests.shelf}/Audit"));
    }

    [Test]
    public void ExtendedBy_adds_the_rules_of_the_block_one_time() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(root => {
            root.AddGroup<Shelf>().Add<Drawer>();
            root.Add<Desk>().Add<Drawer>();
            root.For<Drawer>().ExtendedBy<Audited>();
        }));

        var audit = resolved.Parts.First(p => p.Concept is Audit);
        var drawer = audit.Parent!;

        Assert.That(resolved.UseRules.Count(rule => rule.From == audit && rule.To == drawer), Is.EqualTo(1));
    }

    [Test]
    public void Two_Add_calls_of_one_concept_fold_into_one_part() {
        Part? first = null;
        Part? second = null;
        _ = new ResolvedArchitecture(new InlineArchitecture(root => {
            first = root.Add<Desk>();
            second = root.Add<Desk>();
        }));

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void Two_concepts_with_one_folder_under_one_parent_are_an_error() {
        var error = Assert.Throws<InvalidOperationException>(() => Paths(root => root.Add<Desk>().And<OtherDesk>()))!;

        Assert.That(error.Message, Does.Contain("have the same folder \"Desk\""));
    }

    [Test]
    public void A_find_closes_a_block_place_again_after_its_add() {
        var error = Assert.Throws<InvalidOperationException>(() => Paths(root => {
            var cabinet = root.AddBlock<Cabinet>();
            root.For<Cabinet>().Add<Label>();
            cabinet.Add<Desk>();
        }))!;

        Assert.That(error.Message, Does.Contain("is the place of another block"));
    }

    [Test]
    public void An_add_into_a_closed_block_place_is_an_error_also_for_a_concept_of_its_body() {
        Assert.Throws<InvalidOperationException>(() => Paths(root => root.AddBlock<Cabinet>().Add<Drawer>()));
    }

    [Test]
    public void Two_places_of_one_block_in_two_folders_are_two_places() {
        var paths = Paths(root => {
            root.AddBlock<Cabinet>("Left");
            root.AddBlock<Cabinet>("Right");
        });

        Assert.That(paths, Does.Contain("Left").And.Contain("Right"));
    }

    [Test]
    public void ExtendedBy_on_two_targets_adds_the_rules_for_each_target() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(root => {
            root.Add<Desk>().Add<Drawer>();
            root.AddGroup<Shelf>().Add<Drawer>();
            root.For<Desk>().ExtendedBy<Audited>();
            root.For<Shelf>().ExtendedBy<Audited>();
        }));

        var audits = resolved.Parts.Where(p => p.Concept is Audit);
        var counts = audits.Select(audit => resolved.UseRules.Count(rule => rule.From == audit));

        Assert.That(counts, Has.All.GreaterThan(0));
    }

    [Test]
    public void ExtendedBy_two_times_on_one_part_stacks_the_body_one_time() {
        var paths = Paths(root => {
            root.Add<Desk>().Add<Drawer>();
            root.For<Desk>().ExtendedBy<Audited>();
            root.For<Desk>().ExtendedBy<Audited>();
        });

        Assert.That(paths.Count(path => path == "Desk/Audit"), Is.EqualTo(1));
    }

    [Test]
    public void ExtendedBy_two_times_adds_the_rules_of_the_body_one_time() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(root => {
            root.Add<Desk>().Add<Drawer>();
            root.For<Desk>().ExtendedBy<Audited>();
            root.For<Desk>().ExtendedBy<Audited>();
        }));
        var audit = resolved.Parts.Single(p => p.Concept is Audit);
        var drawer = resolved.Parts.Single(p => p.Concept is Drawer);

        Assert.That(resolved.UseRules.Count(rule => rule.From == audit && rule.To == drawer), Is.EqualTo(1));
    }

    [Test]
    public void And_after_a_find_adds_a_sibling_inside_the_found_block_place() {
        var paths = Paths(root => {
            root.AddBlock<Cabinet>();
            root.For<Cabinet>().Add<Label>().And<Desk>();
        });

        Assert.That(paths, Does.Contain("Cabinet/Label").And.Contain("Cabinet/Desk"));
    }

    [Test]
    public void Add_after_ExtendedBy_adds_a_part_inside_the_found_block_place() {
        var paths = Paths(root => {
            root.AddBlock<Cabinet>();
            root.For<Cabinet>().ExtendedBy<Audited>().Add<Label>();
        });

        Assert.That(paths, Does.Contain("Cabinet/Label").And.Contain("Cabinet/Audit"));
    }

    [Test]
    public void Two_AddGroup_calls_of_one_concept_fold_into_one_group() {
        var paths = Paths(root => {
            root.AddGroup<Shelf>();
            root.AddGroup<Shelf>();
        });

        Assert.That(paths.Count(path => path.StartsWith("Shelves/", StringComparison.Ordinal)), Is.EqualTo(1));
    }

    [Test]
    public void A_part_and_a_block_place_of_one_concept_in_one_folder_are_an_error() {
        Assert.Throws<InvalidOperationException>(() => Paths(root => {
            root.Add<Cabinet>();
            root.AddBlock<Cabinet>();
        }));
    }

    [Test]
    public void Two_concepts_with_one_path_through_a_part_with_no_folder_are_an_error() {
        Assert.Throws<InvalidOperationException>(() => Paths(root => {
            root.Add<Desk>().InParentFolder().Add<Label>();
            root.Add<OtherLabel>();
        }));
    }

    static List<string> Paths(Action<Part> define) =>
        [.. new ResolvedArchitecture(new InlineArchitecture(define)).Parts.Select(p => p.Path)];

    public sealed record Shelf : Concept {
        public override string GroupFolder => "Shelves";
    }

    public sealed record Drawer : Concept;

    public sealed record Desk : Concept;

    public sealed record OtherDesk : Concept {
        public override string Folder => "Desk";
    }

    public sealed record Label : Concept;

    public sealed record OtherLabel : Concept {
        public override string Folder => "Label";
    }

    public sealed record Cabinet : Block {
        protected override void Define(Part part) => part.Add<Drawer>();
    }

    public sealed record Audit : Concept;

    public sealed record Audited : Block {
        protected override void Define(Part part) => part.Add<Audit>();

        protected override void Rules(RuleBook rules) => rules.For<Audit>().Uses<Drawer>();
    }
}
