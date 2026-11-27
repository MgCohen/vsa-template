namespace EngineCore.Tests;

extern alias testing;

using Umpire.Architecture;
using testing::Umpire.Architecture;

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

    [TestCase(typeof(SampleNote))]
    [TestCase(typeof(SampleRate))]
    [TestCase(typeof(SampleKind))]
    [TestCase(typeof(SampleFixed))]
    public void A_type_with_no_member_that_can_change_is_immutable(Type type) {
        Assert.That(Problems(At(type, "Boxes/a/Fixed")), Has.None.Contains("not immutable"));
    }

    [TestCase(typeof(SampleOpen), "Field, Changed, Set")]
    [TestCase(typeof(SampleSlot), "Index")]
    [TestCase(typeof(ISampleOpen), "Set")]
    public void A_member_that_can_change_is_not_immutable(Type type, string members) {
        Assert.That(Problems(At(type, "Boxes/a/Fixed")), Has.Some.Contains($"not immutable ({members} can change)"));
    }

    [TestCase(typeof(SampleNote), true)]
    [TestCase(typeof(SampleRate), true)]
    [TestCase(typeof(SampleFixed), false)]
    [TestCase(typeof(SamplePlain), false)]
    public void A_record_class_and_a_record_struct_are_records(Type type, bool isRecord) {
        Assert.That(Problems(At(type, "Boxes/a/Data")), isRecord ? Is.Empty : Has.Some.Contains("it is not record"));
    }

    public record SampleNote(string Text);

    public readonly record struct SampleRate(int Percent);

    public record struct SampleSlot(int Index);

    public readonly struct SamplePlain;

    public enum SampleKind { None }

    public interface ISampleOpen {
        int Set { get; set; }
    }

    public sealed class SampleFixed(int seed) {
        public const int Size = 1;
        public static readonly int Zero = 0;

        public int Init { get; init; }
        public int Get { get; }
        public int Seed => seed;
    }

    public sealed class SampleOpen {
        public int Field;

        public event Action? Changed;

        public int Set { get; private set; }

        public void Change() => Changed?.Invoke();
    }

    public delegate void SamplePing();

    public sealed class SampleSealed;

    public sealed class FactsArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            var box = block.AddGroup<Box>();
            box.Add<Item>().And<Tag>().InParentFolder();
            box.Add<Fixed>().And<Data>();
        }

        protected override void Rules(RuleBook rules) {
            rules.Type<System.Text.StringBuilder>().OnlyIn<Item>();
            rules.For<Box>().Are(!Trait.Sealed);
            rules.For<Fixed>().Are<Umpire.Architecture.Traits.Immutable>();
            rules.For<Data>().Are<Umpire.Architecture.Traits.Record>();
        }
    }

    public sealed record Fixed : Concept;

    public sealed record Data : Concept;
}
