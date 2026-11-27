namespace EngineCore.Tests;

extern alias testing;

using Umpire.Architecture;
using static EngineCore.Tests.ContentTests;
using testing::Umpire.Architecture;

public sealed class ContentPlaceTests : ArchitectureTest<ContentPlaceTests.MarkOwnerArchitecture> {
    [Test]
    public void A_derived_attribute_is_an_owned_attribute() {
        Assert.That(Problems(At<MarkedByDerived>("Boxes/a")), Is.EqualTo(new[] {
            "MarkedByDerived in Box (Boxes/a) has [MarkAttribute], but the rule \"MarkAttribute is only in Item\" keeps it in Item. "
            + "Move the code with [MarkAttribute] to Item, or remove the attribute.",
        }));
    }

    public sealed class MarkOwnerArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            block.AddGroup<Box>().Add<Item>().And<Tag>().InParentFolder();
        }

        protected override void Rules(RuleBook rules) {
            rules.Type<MarkAttribute>().OnlyIn<Item>();
        }
    }
}
