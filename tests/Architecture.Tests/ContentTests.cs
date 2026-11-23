namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Architecture.Traits;
using static EngineCore.Tests.CountArchitecture;

public sealed class ContentTests {
    static readonly string Mark = typeof(MarkAttribute).FullName!;

    [TestCase("ItemHook", true)]
    [TestCase("ItemHook`1", true)]
    [TestCase("ItemA", false)]
    public void A_name_end_rule_checks_the_end_of_the_name(string name, bool fits) {
        var results = Named(item => item.NameEndsWith("Hook")).Names(Item(name));

        Assert.That(results, fits ? Is.Empty : Has.Count.EqualTo(1));
    }

    [Test]
    public void A_wrong_name_gives_the_rule_and_the_name_to_use() {
        var result = Named(item => item.NameEndsWith("Hook")).Names(Item("ItemA")).Single();

        Assert.That(result.Message, Is.EqualTo(
            "ItemA is in Item (Boxes/a), but the name of a Item type ends with Hook. Rename it to ItemAHook, or move it."));
    }

    [Test]
    public void A_name_start_rule_checks_the_start_of_the_name() {
        var definition = Named(item => item.NameStartsWith("I"));

        Assert.Multiple(() => {
            Assert.That(definition.Names(Item("IClock")), Is.Empty);
            Assert.That(definition.Names(Item("Clock")).Single().Message, Does.Contain("starts with I. Rename it to IClock"));
        });
    }

    [Test]
    public void A_name_start_rule_and_a_name_end_rule_join() {
        var definition = Named(item => item.NameStartsWith("I").NameEndsWith("Source"));

        Assert.That(definition.Names(Item("Clock")).Single().Message,
            Does.Contain("starts with I and ends with Source. Rename it to IClockSource"));
    }

    [Test]
    public void A_group_name_rule_takes_the_name_of_the_group_folder() {
        var definition = Named(item => item.NamedAfterGroup());

        Assert.Multiple(() => {
            Assert.That(definition.Names(Item("Touch", "Touch")), Is.Empty);
            Assert.That(definition.Names(Item("Taps", "Touch")).Single().Message, Does.Contain("is <group>. Rename it to Touch"));
        });
    }

    [Test]
    public void A_group_name_rule_can_have_a_suffix() {
        var definition = Named(item => item.NamedAfterGroup("Settings"));

        Assert.Multiple(() => {
            Assert.That(definition.Names(Item("TouchSettings", "Touch")), Is.Empty);
            Assert.That(definition.Names(Item("Touch", "Touch")).Single().Message,
                Does.Contain("is <group>Settings. Rename it to TouchSettings"));
        });
    }

    [Test]
    public void A_group_name_rule_can_name_its_group() {
        var definition = Named(item => item.NamedAfterGroup<Box>("Settings"));

        Assert.That(definition.Names(Item("Touch", "Touch")).Single().Message,
            Does.Contain("is <Box>Settings. Rename it to TouchSettings"));
    }

    [Test]
    public void A_group_name_rule_keeps_an_earlier_end() {
        var definition = Named(item => item.NameEndsWith("Settings").NamedAfterGroup());

        Assert.That(definition.Names(Item("TouchSettings", "Touch")), Is.Empty);
    }

    [Test]
    public void A_group_name_rule_on_a_part_with_no_group_stops_the_definition() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new NoGroupArchitecture()));

        Assert.That(error!.Message, Does.StartWith("Define: Item has the name rule \"is <group>\", but it is not under a group."));
    }

    [Test]
    public void A_trait_rule_fails_each_type_without_the_trait() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Are<Static>());

        var results = definition.Traits(Item("Plain"), Item("Tool") with { IsStatic = true });

        Assert.That(results.Single().Message, Is.EqualTo(
            "Plain is in Item (Boxes/a), but it is not static, so it breaks the rule \"Item types are static\". "
            + "Change the type, or move it to another part."));
    }

    [Test]
    public void A_trait_chain_needs_each_trait() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Are<Sealed>().And<Record>());

        var results = definition.Traits(Item("Plain") with { IsSealed = true }, Item("Data") with { IsSealed = true, IsRecord = true });

        Assert.That(results.Single().Message, Does.StartWith("Plain is in Item (Boxes/a), but it is not record, so it breaks"));
    }

    [Test]
    public void An_or_condition_takes_either_trait() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Are(Trait.Static | Trait.Enum));

        Assert.Multiple(() => {
            Assert.That(definition.Traits(Item("Color") with { IsEnum = true }), Is.Empty);
            Assert.That(definition.Traits(Item("Plain")).Single().Rule, Is.EqualTo("Item types are static or enum"));
        });
    }

    [Test]
    public void A_not_condition_refuses_the_trait() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Are(!Trait.Abstract));

        Assert.Multiple(() => {
            Assert.That(definition.Traits(Item("Plain")), Is.Empty);
            Assert.That(definition.Traits(Item("Base") with { IsAbstract = true }).Single().Rule, Is.EqualTo("Item types are not abstract"));
        });
    }

    [Test]
    public void A_not_condition_names_the_trait_that_the_type_has() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Are(!Trait.Abstract));

        Assert.That(definition.Traits(Item("Base") with { IsAbstract = true }).Single().Message, Is.EqualTo(
            "Base is in Item (Boxes/a), but it is abstract, so it breaks the rule \"Item types are not abstract\". "
            + "Change the type, or move it to another part."));
    }

    [Test]
    public void A_mixed_condition_shows_its_groups() {
        Assert.That(((Trait.Sealed & Trait.Record) | !(Trait.Struct | Trait.Enum)).ToString(),
            Is.EqualTo("(sealed and record) or not (struct or enum)"));
    }

    [Test]
    public void Two_instances_of_a_trait_are_equal() {
        Assert.That(Trait.Sealed, Is.EqualTo(new Sealed()));
    }

    [Test]
    public void An_interface_is_not_abstract_for_the_trait() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Are<Abstract>());

        Assert.That(definition.Traits(Item("IItem") with { IsAbstract = true, IsInterface = true }), Has.Count.EqualTo(1));
    }

    [Test]
    public void An_attribute_in_another_part_fails() {
        var definition = new CountArchitecture(rules => rules.Type<MarkAttribute>().OnlyIn<Item>());

        var results = definition.Places(Item("ItemA") with { Attributes = [Mark] }, MarkedTag());

        Assert.That(results.Single().Message, Is.EqualTo(
            "TagA in Tag (Boxes/a) has [MarkAttribute], but the rule \"MarkAttribute is only in Item\" keeps it in Item. "
            + "Move the code with [MarkAttribute] to Item, or remove the attribute."));
    }

    [Test]
    public void A_type_with_two_places_can_be_in_each_of_them() {
        var definition = new CountArchitecture(rules => rules.Type<MarkAttribute>().OnlyIn<Item>().And<Tag>());

        Assert.That(definition.Places(Item("ItemA") with { Attributes = [Mark] }, MarkedTag()), Is.Empty);
    }

    [Test]
    public void Two_place_rules_of_one_type_join_their_places() {
        var definition = new CountArchitecture(rules => {
            rules.Type<MarkAttribute>().OnlyIn<Item>();
            rules.Type<MarkAttribute>().OnlyIn<Tag>();
        });

        Assert.That(definition.Places(Item("ItemA") with { Attributes = [Mark] }, MarkedTag()), Is.Empty);
    }

    [Test]
    public void A_use_of_a_type_in_another_part_fails() {
        var used = typeof(System.Text.StringBuilder).FullName!;
        var definition = new CountArchitecture(rules => rules.Type<System.Text.StringBuilder>().OnlyIn<Item>());

        var results = definition.Places(Item("ItemA") with { References = [used] }, Fact("TagA", "a", typeof(ITagged)) with { References = [used] });

        Assert.That(results.Single().Message, Is.EqualTo(
            "TagA in Tag (Boxes/a) uses StringBuilder, but the rule \"StringBuilder is only in Item\" keeps it in Item. "
            + "Move the code that uses StringBuilder to Item, or remove the use."));
    }

    [Test]
    public void A_base_class_is_a_use() {
        var definition = new CountArchitecture(rules => rules.Type<Tool>().OnlyIn<Item>());
        var derived = Fact("TagA", "a", typeof(ITagged)) with { Bases = [typeof(ITagged).FullName!, typeof(Tool).FullName!] };

        Assert.That(definition.Places(derived), Has.Count.EqualTo(1));
    }

    [Test]
    public void The_type_itself_is_not_a_use() {
        var definition = new CountArchitecture(rules => rules.Type<MarkAttribute>().OnlyIn<Item>());
        var declaration = Fact("MarkAttribute", "a", typeof(ITagged)) with { FullName = Mark, Bases = [Mark] };

        Assert.That(definition.Places(declaration), Is.Empty);
    }

    [Test]
    public void A_place_rule_of_a_concept_keeps_the_places_under_the_concept() {
        var resolved = new ResolvedArchitecture(new ShelfArchitecture());
        var onShelf = Marked("ItemA", "Shelves/s/Item");
        var inCrate = Marked("ItemB", "Crates/c/Item");

        var results = resolved.Places(resolved.Place([onShelf, inCrate], BlockFolder));

        Assert.That(results.Single().Message, Does.StartWith("ItemB in Item (Crates/c/Item) has [MarkAttribute]"));
    }

    [Test]
    public void A_place_rule_of_a_concept_refuses_a_place_outside_the_concept() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new LooseShelfArchitecture()));

        Assert.That(error!.Message, Does.StartWith("Rules of ContentTests.LooseShelf: OnlyIn<Tag>() names no part under ContentTests.LooseShelf."));
    }

    [Test]
    public void The_name_and_trait_checks_do_not_read_a_nested_type() {
        var mode = Item("Mode", "Touch") with { IsEnum = true, IsNested = true };
        var definition = new CountArchitecture(
            rules => rules.For<Item>().Are<Static>(),
            box => box.Add<Item>().NamedAfterGroup().And<Tag>().InParentFolder());

        Assert.Multiple(() => {
            Assert.That(definition.Names(mode), Is.Empty);
            Assert.That(definition.Traits(mode), Is.Empty);
        });
    }

    [Test]
    public void The_place_check_reads_a_nested_type() {
        var definition = new CountArchitecture(rules => rules.Type<MarkAttribute>().OnlyIn<Item>());
        var nested = MarkedTag() with { IsNested = true };

        Assert.That(definition.Places(nested), Has.Count.EqualTo(1));
    }

    [Test]
    public void A_generic_attribute_matches_its_closed_form() {
        var definition = new CountArchitecture(rules => rules.Type<GroupMarkAttribute<int>>().OnlyIn<Item>());
        var tag = Fact("TagA", "a", typeof(ITagged)) with { Attributes = [typeof(GroupMarkAttribute<>).FullName!] };

        Assert.That(definition.Places(tag).Single().Message, Does.StartWith("TagA in Tag (Boxes/a) has [GroupMarkAttribute]"));
    }

    [Test]
    public void A_static_class_counts() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>(Depth.Self).AtMost(1));

        var results = definition.Counts(Item("One") with { IsStatic = true }, Item("Two") with { IsStatic = true });

        Assert.That(results.Single().Problem, Is.EqualTo(CountProblem.TooMany));
    }

    static CountArchitecture Named(Func<PartGroup, PartGroup> name) =>
        new(parts: box => name(box.Add<Item>()).And<Tag>().InParentFolder());

    static TypeFacts Item(string name, string box = "a") => Fact(name, box, typeof(ICounted));

    static TypeFacts MarkedTag() => Fact("TagA", "a", typeof(ITagged)) with { Attributes = [Mark] };

    [AttributeUsage(AttributeTargets.All)]
    public class MarkAttribute : Attribute;

    public sealed class DerivedMarkAttribute : MarkAttribute;

    [AttributeUsage(AttributeTargets.All)]
    public sealed class GroupMarkAttribute<T> : Attribute;

    [DerivedMark]
    public sealed class MarkedByDerived;

    public class Tool;

    public sealed record Shelf : Block {
        public override string GroupFolder => "Shelves";

        protected override void Rules(RuleBook rules) {
            rules.Type<MarkAttribute>().OnlyIn<Item>();
        }
    }

    public sealed record LooseShelf : Block {
        public override string GroupFolder => "Shelves";

        protected override void Rules(RuleBook rules) {
            rules.Type<MarkAttribute>().OnlyIn<Tag>();
        }
    }

    public sealed record Crate : Concept {
        public override string GroupFolder => "Crates";
    }

    static TypeFacts Marked(string name, string folder) =>
        new($"App.{name}", name, "App", new Source($"{BlockFolder}/{folder}/{name}.cs", 1), [], [], [], []) { Attributes = [Mark] };

    sealed class ShelfArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            block.AddGroup<Shelf>().Add<Item>();
            block.AddGroup<Crate>().Add<Item>();
        }
    }

    sealed class LooseShelfArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            block.AddGroup<LooseShelf>().Add<Item>();
            block.AddGroup<Crate>().Add<Tag>();
        }
    }

    sealed class NoGroupArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            block.Add<Item>().NamedAfterGroup();
        }
    }
}
