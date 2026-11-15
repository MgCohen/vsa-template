using VSA.Architecture;

namespace EngineCore.Tests;

public sealed class PartChainTests {
    [Test]
    public void AddGroup_on_an_And_result_adds_a_group_under_each_part() {
        var root = new InlineArchitecture(root => root.Add<Target>().And<User>().AddGroup<Group>().Add<Child>()).Root;

        Assert.That(ParentNames<Group>(root), Is.EqualTo(new[] { "Target", "User" }));
        Assert.That(ParentNames<Child>(root), Is.EqualTo(new[] { "Group", "Group" }));
    }

    [Test]
    public void OfType_on_an_And_result_applies_to_each_part() {
        var root = new InlineArchitecture(root => root.Add<Target>().And<User>().OfType<string>()).Root;

        Assert.That(PartsOf<Target>(root).Concat(PartsOf<User>(root)).Select(p => p.Types.Single()), Is.EqualTo(new[] { typeof(string), typeof(string) }));
    }

    [Test]
    public void And_on_an_AddGroup_result_gives_a_sibling_of_the_group() {
        var root = new InlineArchitecture(root => root.AddGroup<Group>().And<Child>().OfType<string>()).Root;

        Assert.That(ParentNames<Child>(root), Is.EqualTo(new[] { "(root)" }));
        Assert.That(PartsOf<Group>(root).Single().Types, Is.EqualTo(new[] { typeof(string) }));
    }

    [Test]
    public void AddGroup_with_a_boundary_sets_the_boundary_of_each_group() {
        var root = new InlineArchitecture(root => root.Add<Target>().And<User>().AddGroup<Group>(Boundary.Closed)).Root;

        Assert.That(PartsOf<Group>(root).Select(p => p.Boundary), Is.EqualTo(new Boundary?[] { Boundary.Closed, Boundary.Closed }));
    }

    [Test]
    public void A_part_and_a_part_group_have_the_same_chain() {
        IPartChain? fromPart = null;
        IPartChain? fromGroup = null;
        _ = new InlineArchitecture(root => {
            fromPart = root.Add<Target>();
            fromGroup = root.AddGroup<Group>();
        }).Root;

        Assert.That(fromPart, Is.TypeOf<Part>());
        Assert.That(fromGroup, Is.TypeOf<PartGroup>());
    }

    static IEnumerable<Part> PartsOf<T>(Root root) where T : Concept => root.Walk().Where(p => p.Is(typeof(T)));

    static IEnumerable<string> ParentNames<T>(Root root) where T : Concept => PartsOf<T>(root).Select(p => p.Parent!.Name);
}
