namespace EngineCore.Tests;

using Umpire.Architecture;
using static EngineCore.Tests.CountArchitecture;

public sealed class ContentTests {
    static readonly string Mark = typeof(MarkAttribute).FullName!;

    [TestCase("ItemHook", true)]
    [TestCase("ItemHook`1", true)]
    [TestCase("ItemA", false)]
    public void A_suffix_name_rule_checks_the_end_of_the_name(string name, bool fits) {
        var results = Named(Name.Suffix("Hook")).Names(Item(name));

        Assert.That(results, fits ? Is.Empty : Has.Count.EqualTo(1));
    }

    [Test]
    public void A_wrong_name_gives_the_rule_and_the_name_to_use() {
        var result = Named(Name.Suffix("Hook")).Names(Item("ItemA")).Single();

        Assert.That(result.Message, Is.EqualTo(
            "ItemA is in Item (Boxes/a), but the name of a Item type ends with Hook. Rename it to ItemAHook, or move it."));
    }

    [Test]
    public void A_prefix_name_rule_checks_the_start_of_the_name() {
        var definition = Named(Name.Prefix("I"));

        Assert.Multiple(() => {
            Assert.That(definition.Names(Item("IClock")), Is.Empty);
            Assert.That(definition.Names(Item("Clock")).Single().Message, Does.Contain("starts with I. Rename it to IClock"));
        });
    }

    [Test]
    public void A_group_name_rule_takes_the_name_of_the_group_instance() {
        var definition = Named(Name.OfGroup());

        Assert.Multiple(() => {
            Assert.That(definition.Names(Item("Touch", "Touch")), Is.Empty);
            Assert.That(definition.Names(Item("Taps", "Touch")).Single().Message, Does.Contain("is <group>. Rename it to Touch"));
        });
    }

    [Test]
    public void A_group_name_rule_can_have_a_suffix() {
        var definition = Named(Name.OfGroup<Box>().WithSuffix("Settings"));

        Assert.Multiple(() => {
            Assert.That(definition.Names(Item("TouchSettings", "Touch")), Is.Empty);
            Assert.That(definition.Names(Item("Touch", "Touch")).Single().Message,
                Does.Contain("is <Box>Settings. Rename it to TouchSettings"));
        });
    }

    [Test]
    public void A_group_name_rule_on_a_part_with_no_group_stops_the_definition() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new NoGroupArchitecture()));

        Assert.That(error!.Message, Does.StartWith("Define: Item has Named(is <group>), but it is not under a group."));
    }

    [Test]
    public void A_kind_rule_fails_each_type_of_another_kind() {
        var definition = new CountArchitecture(rules => rules.For<Item>().OnlyKind(TypeKind.StaticClass));

        var results = definition.Kinds(Item("Plain"), Item("Tool") with { Kind = TypeKind.StaticClass });

        Assert.That(results.Single().Message, Is.EqualTo(
            "Plain is in Item (Boxes/a), but it is a Class: the rule \"Item has only StaticClass\" allows only StaticClass. "
            + "Change the type, or move it to another part."));
    }

    [Test]
    public void A_kind_rule_can_allow_more_than_one_kind() {
        var definition = new CountArchitecture(rules => rules.For<Item>().OnlyKind(TypeKind.StaticClass | TypeKind.Enum));

        Assert.That(definition.Kinds(Item("Color") with { Kind = TypeKind.Enum }), Is.Empty);
    }

    [Test]
    public void A_kind_rule_of_None_allows_no_type() {
        var definition = new CountArchitecture(rules => rules.For<Item>().OnlyKind(TypeKind.None));

        Assert.That(definition.Kinds(Item("Tool") with { Kind = TypeKind.StaticClass }).Single().Message,
            Does.Contain("the rule \"Item has no type\" allows no type. Move it to another part."));
    }

    [Test]
    public void An_owned_attribute_in_another_part_fails() {
        var definition = new CountArchitecture(rules => rules.For<Item>().Owns<MarkAttribute>());

        var results = definition.Owners(Item("ItemA") with { Attributes = [Mark] }, MarkedTag());

        Assert.That(results.Single().Message, Is.EqualTo(
            "TagA in Tag (Boxes/a) has [MarkAttribute], but the rule \"Item owns MarkAttribute\" keeps it in Item. "
            + "Move the code with [MarkAttribute] to Item, or remove the attribute."));
    }

    [Test]
    public void An_attribute_with_two_owners_can_be_in_each_of_them() {
        var definition = new CountArchitecture(rules => {
            rules.For<Item>().Owns<MarkAttribute>();
            rules.For<Tag>().Owns<MarkAttribute>();
        });

        Assert.That(definition.Owners(Item("ItemA") with { Attributes = [Mark] }, MarkedTag()), Is.Empty);
    }

    [Test]
    public void A_static_class_counts() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>(Depth.Self).AtMost(1));

        var results = definition.Counts(
            Item("One") with { Kind = TypeKind.StaticClass }, Item("Two") with { Kind = TypeKind.StaticClass });

        Assert.That(results.Single().Problem, Is.EqualTo(CountProblem.TooMany));
    }

    static CountArchitecture Named(Name name) =>
        new(parts: box => box.Add<Item>().Named(name).And<Tag>().InParentFolder());

    static TypeFacts Item(string name, string box = "a") => Fact(name, box, typeof(ICounted));

    static TypeFacts MarkedTag() => Fact("TagA", "a", typeof(ITagged)) with { Attributes = [Mark] };

    [AttributeUsage(AttributeTargets.All)]
    public sealed class MarkAttribute : Attribute;

    sealed class NoGroupArchitecture : Architecture {
        protected override void Define(Block block) {
            block.Add<Item>().Named(Name.OfGroup());
        }
    }
}
