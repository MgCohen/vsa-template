namespace EngineCore.Tests;

using Umpire.Architecture;
using static EngineCore.Tests.CountArchitecture;

public sealed class MatchTests {
    static readonly TypeFacts ItemA = Fact("ItemA", "a", typeof(ICounted));
    static readonly TypeFacts ItemB = Fact("ItemB", "b", typeof(ICounted));

    static CountArchitecture TagsTakeItemsInTheirBox => new(rules => rules.For<Tag>().Takes<Item>().InSame<Box>());

    [Test]
    public void A_type_argument_in_the_same_instance_passes() {
        var tag = Fact("TagA", "a", typeof(ITagged), ItemA.FullName);

        Assert.That(TagsTakeItemsInTheirBox.Matches(ItemA, tag), Is.Empty);
    }

    [Test]
    public void A_type_argument_in_another_instance_fails_at_the_subject() {
        var tag = Fact("TagA", "a", typeof(ITagged), ItemB.FullName);

        var result = TagsTakeItemsInTheirBox.Matches(ItemB, tag).Single();

        Assert.That(result.Subject.Type, Is.EqualTo(tag));
        Assert.That(result.Taken!.Type, Is.EqualTo(ItemB));
        Assert.That(result.Message, Does.StartWith("TagA in Tag (Boxes/a) takes ItemB in Item (Boxes/b): "
            + "the rule \"Tag takes Item in same Box\" needs both in one Box."));
    }

    [Test]
    public void A_static_class_is_not_a_subject() {
        var helper = Fact("TagTools", "a", typeof(ITagged)) with { Kind = TypeKind.StaticClass };

        Assert.That(TagsTakeItemsInTheirBox.Matches(ItemA, helper), Is.Empty);
    }

    [Test]
    public void A_subject_with_no_type_argument_of_the_concept_fails() {
        var tag = Fact("TagA", "a", typeof(ITagged));

        var result = TagsTakeItemsInTheirBox.Matches(ItemA, tag).Single();

        Assert.That(result.Taken, Is.Null);
        Assert.That(result.Message, Does.Contain("takes no Item type"));
    }

    [Test]
    public void Takes_with_no_InSame_needs_only_a_type_argument_of_the_concept() {
        var definition = new CountArchitecture(rules => rules.For<Tag>().Takes<Item>());
        var tag = Fact("TagA", "a", typeof(ITagged), ItemB.FullName);

        Assert.That(definition.Matches(ItemB, tag), Is.Empty);
    }

    [Test]
    public void InSame_with_a_concept_that_is_not_a_group_is_an_error_of_the_definition() {
        var definition = new CountArchitecture(rules => rules.For<Tag>().Takes<Item>().InSame<Item>());

        Assert.That(() => definition.Matches(ItemA),
            Throws.InvalidOperationException.With.Message.Contains("InSame<Item> needs a concept that AddGroup adds."));
    }
}
