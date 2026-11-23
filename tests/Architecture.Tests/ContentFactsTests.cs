namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class ContentFactsTests : ArchitectureTest<ContentFactsTests.FactsArchitecture> {
    [Test]
    public void A_member_that_the_compiler_makes_is_not_a_use() {
        Assert.That(Problems(At<SampleNote>("Boxes/a")), Is.Empty);
    }

    [Test]
    public void A_delegate_is_not_sealed() {
        Assert.That(Problems(At<SamplePing>("Boxes/a")), Is.Empty);
    }

    [Test]
    public void A_sealed_class_has_the_sealed_trait() {
        Assert.That(Problems(At<SampleSealed>("Boxes/a")), Has.Some.Contains("but it is sealed"));
    }

    public record SampleNote(string Text);

    public delegate void SamplePing();

    public sealed class SampleSealed;

    public sealed class FactsArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            block.AddGroup<Box>().Add<Item>().And<Tag>().InParentFolder();
        }

        protected override void Rules(RuleBook rules) {
            rules.Type<System.Text.StringBuilder>().OnlyIn<Item>();
            rules.For<Box>().Are(!Trait.Sealed);
        }
    }
}
