namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class ClosedBaseTests {
    [Test]
    public void The_closed_base_is_found_two_levels_up() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(Box<>)), Is.EqualTo(typeof(Box<int>)));
    }

    [Test]
    public void The_type_itself_is_not_its_own_closed_base() {
        Assert.That(ClosedBase.Of(typeof(Box<int>), typeof(Box<>)), Is.Null);
    }

    [Test]
    public void The_self_or_base_walk_matches_the_type_itself() {
        Assert.That(ClosedBase.OfSelfOrBase(typeof(Box<int>), typeof(Box<>)), Is.EqualTo(typeof(Box<int>)));
    }

    [Test]
    public void A_closed_definition_matches_no_base() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(Box<int>)), Is.Null);
        Assert.That(ClosedBase.OfSelfOrBase(typeof(Box<int>), typeof(Box<int>)), Is.Null);
    }

    [Test]
    public void The_base_walk_does_not_match_an_interface() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(IHolder<>)), Is.Null);
    }

    [Test]
    public void The_closed_interface_is_found_on_a_derived_type() {
        Assert.That(ClosedBase.InterfaceOf(typeof(Grandchild), typeof(IHolder<>)), Is.EqualTo(typeof(IHolder<string>)));
    }

    [Test]
    public void A_type_with_no_match_has_no_closed_base() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(List<>)), Is.Null);
        Assert.That(ClosedBase.InterfaceOf(typeof(Grandchild), typeof(List<>)), Is.Null);
    }

    public abstract class Box<T>;

    public interface IHolder<T>;

    public class Child : Box<int>, IHolder<string>;

    public sealed class Grandchild : Child;
}
