namespace EngineCore.Tests;

using Umpire.Architecture;
using static EngineCore.Tests.CountArchitecture;

public sealed class CountTests {
    static readonly TypeFacts ItemA = Fact("ItemA", "a", typeof(ICounted));
    static readonly TypeFacts SecondItemA = Fact("SecondItemA", "a", typeof(ICounted));
    static readonly TypeFacts SmallA = Fact("SmallA", "a", typeof(ISmallCounted));
    static readonly TypeFacts TagB = Fact("TagB", "b", typeof(ITagged));

    [Test]
    public void A_group_instance_with_fewer_types_than_AtLeast_fails_at_its_first_type() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>().AtLeast(1));

        var results = definition.Counts(ItemA, TagB);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Problem, Is.EqualTo(CountProblem.TooFew));
        Assert.That(results[0].At.Type, Is.EqualTo(TagB));
        Assert.That(results[0].Message, Does.StartWith("Box App/Boxes/b has 0 Item types: the rule \"Box has Item at least 1\" needs at least 1."));
    }

    [Test]
    public void A_group_instance_with_more_types_than_AtMost_fails() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>(Depth.Self).AtMost(1));

        var results = definition.Counts(ItemA, SecondItemA);

        Assert.That(results.Select(r => r.Problem), Is.EqualTo(new[] { CountProblem.TooMany }));
        Assert.That(results[0].Found.Select(b => b.Type.Name), Is.EquivalentTo(new[] { "ItemA", "SecondItemA" }));
    }

    [TestCase(null, 2)]
    [TestCase(Depth.WithChildren, 2)]
    [TestCase(Depth.Self, 1)]
    public void A_count_takes_the_nested_concepts_unless_the_depth_is_Self(Depth? depth, int counted) {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>(depth).Exactly(0));

        var results = definition.Counts(ItemA, SmallA);

        Assert.That(results.Single().Found, Has.Count.EqualTo(counted));
    }

    [Test]
    public void A_helper_that_follows_its_user_is_not_counted() {
        var helper = Fact("ItemHelper", "a");
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>(Depth.Self).Exactly(1));

        Assert.That(definition.Counts(Using(ItemA, helper), helper), Is.Empty);
    }

    [Test]
    public void Two_kinds_in_one_instance_fail_OfOneKind() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>().OfOneKind());

        var results = definition.Counts(ItemA, SmallA);

        Assert.That(results.Single().Problem, Is.EqualTo(CountProblem.ManyKinds));
        Assert.That(results.Single().Message, Does.Contain("ItemA (Item), SmallA (Item.Small)"));
    }

    [Test]
    public void A_part_subject_counts_the_types_that_take_each_of_its_types() {
        var taker = Fact("TagA", "a", typeof(ITagged), ItemA.FullName);
        var definition = new CountArchitecture(rules => rules.For<Item>().Has<Tag>().Exactly(1));

        Assert.Multiple(() => {
            Assert.That(definition.Counts(ItemA, taker), Is.Empty);
            Assert.That(definition.Counts(ItemA).Single().Message, Does.StartWith("ItemA in Item (App/Boxes/a) has 0 Tag types that take it"));
        });
    }

    [Test]
    public void A_count_takes_only_the_types_under_its_root() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>().AtLeast(1));

        Assert.That(definition.Counts(Outside("ItemOutside")), Is.Empty);
    }

    [Test]
    public void A_count_with_no_limit_is_an_error_of_the_definition() {
        var definition = new CountArchitecture(rules => rules.For<Box>().Has<Item>());

        Assert.That(() => definition.Counts(ItemA),
            Throws.InvalidOperationException.With.Message.Contains("has no limit. Add AtLeast, AtMost, Exactly or OfOneKind."));
    }

    [Test]
    public void Required_at_the_call_site_needs_one_type_of_the_base_in_each_instance() {
        var definition = new CountArchitecture(parts: box => box.Add<Item>().OfType<ICounted>(required: true).And<Tag>().InParentFolder());

        var results = definition.Counts(ItemA, TagB);

        Assert.That(results.Single().Message, Does.StartWith("Box App/Boxes/b has 0 Item types with base ICounted"));
    }

    [Test]
    public void Required_in_Configure_of_the_concept_needs_one_type_in_each_instance() {
        var definition = new CountArchitecture(parts: box => box.Add<NeededByConcept>().And<Tag>().InParentFolder());

        Assert.That(definition.Counts(TagB).Single().Rule, Is.EqualTo("NeededByConcept requires INeeded in each Box"));
    }

    [Test]
    public void Required_in_Configure_of_the_architecture_needs_each_base_in_each_instance() {
        var definition = new CountArchitecture(parts: box => box.Add<Item>().And<Tag>().InParentFolder(), required: true);

        Assert.That(definition.Counts(ItemA, TagB).Select(r => r.Subject + ": " + r.Rule), Is.EquivalentTo(new[] {
            "Box App/Boxes/a: Tag requires ITagged in each Box",
            "Box App/Boxes/b: Item requires ICounted in each Box",
        }));
    }

    [Test]
    public void Required_false_at_the_call_site_wins_over_the_architecture() {
        var definition = new CountArchitecture(
            parts: box => box.Add<Item>().OfType<ICounted>(required: false).And<Tag>().InParentFolder(), required: true);

        Assert.That(definition.Counts(ItemA, TagB).Select(r => r.Rule), Is.EqualTo(new[] { "Tag requires ITagged in each Box" }));
    }

    [Test]
    public void With_no_Required_option_a_part_is_allowed_and_not_required() {
        var definition = new CountArchitecture();

        Assert.That(definition.Counts(TagB), Is.Empty);
    }
}
