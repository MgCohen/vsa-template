namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class PartChainTests {
    [Test]
    public void AddGroup_on_an_And_result_adds_a_group_under_each_part() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.Add<Target>().And<User>().AddGroup<Group>().Add<Child>())).Block;

        Assert.That(ParentNames<Group>(block), Is.EqualTo(new[] { "Target", "User" }));
        Assert.That(ParentNames<Child>(block), Is.EqualTo(new[] { "Group", "Group" }));
    }

    [Test]
    public void OfType_on_an_And_result_applies_to_each_part() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.Add<Target>().And<User>().OfType<string>())).Block;

        Assert.That(PartsOf<Target>(block).Concat(PartsOf<User>(block)).Select(p => p.Types.Single()), Is.EqualTo(new[] { typeof(string), typeof(string) }));
    }

    [Test]
    public void And_on_an_AddGroup_result_gives_a_sibling_of_the_group() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Group>().And<Child>().OfType<string>())).Block;

        Assert.That(ParentNames<Child>(block), Is.EqualTo(new[] { "(block)" }));
        Assert.That(PartsOf<Group>(block).Single().Types, Is.EqualTo(new[] { typeof(string) }));
    }

    [Test]
    public void AddGroup_with_a_boundary_sets_the_boundary_of_each_group() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.Add<Target>().And<User>().AddGroup<Group>(Boundary.Closed))).Block;

        Assert.That(PartsOf<Group>(block).Select(p => p.Options.Boundary), Is.EqualTo(new Boundary?[] { Boundary.Closed, Boundary.Closed }));
    }

    [Test]
    public void A_change_to_the_options_of_a_part_does_not_change_the_part() {
        var block = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Group>(Boundary.Closed))).Block;
        var group = PartsOf<Group>(block).Single();

        group.Options.Boundary = Boundary.Open;

        Assert.That(group.Options.Boundary, Is.EqualTo(Boundary.Closed));
    }

    [Test]
    public void A_part_and_a_part_group_have_the_same_chain() {
        IPartChain? fromPart = null;
        IPartChain? fromGroup = null;
        _ = new ResolvedArchitecture(new InlineArchitecture(block => {
            fromPart = block.Add<Target>();
            fromGroup = block.AddGroup<Group>();
        }));

        Assert.That(fromPart, Is.TypeOf<Part>());
        Assert.That(fromGroup, Is.TypeOf<PartGroup>());
    }

    static IEnumerable<Part> PartsOf<T>(Block block) where T : Concept => block.Walk().Where(p => p.Is(typeof(T)));

    static IEnumerable<string> ParentNames<T>(Block block) where T : Concept => PartsOf<T>(block).Select(p => p.Parent!.Name);
}
