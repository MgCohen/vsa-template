namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class ClosedBaseTests {
    [Test]
    public void The_closed_base_is_found_two_levels_up() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(Box<>)), Is.EqualTo(typeof(Box<int>)));
    }

    [Test]
    public void The_closed_interface_is_found_when_no_base_class_matches() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(IHolder<>)), Is.EqualTo(typeof(IHolder<string>)));
    }

    [Test]
    public void A_type_with_no_match_has_no_closed_base() {
        Assert.That(ClosedBase.Of(typeof(Grandchild), typeof(List<>)), Is.Null);
    }

    public abstract class Box<T>;

    public interface IHolder<T>;

    public class Child : Box<int>, IHolder<string>;

    public sealed class Grandchild : Child;
}
