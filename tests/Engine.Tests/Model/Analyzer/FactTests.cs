namespace CheckModel.Tests;

using Umpire.CheckModel;
using UmpireEngine;

public sealed class FactTests {
    const string Source = """
        namespace Sample;

        using System;
        using System.Collections.Generic;

        public interface IShape { void Draw(Pen pen); }

        public sealed class Pen;

        [Obsolete("old", false)]
        public sealed partial class Circle<T> : Base, IShape where T : class, IShape {
            public const int Sides = 0;
            internal List<Pen> Pens { get; private set; } = [];
            public void Draw(Pen pen) { }
            protected static T? First(ref int count, params T[] all) => null;
        }

        public abstract class Base : IEquatable<Pen> {
            public bool Equals(Pen? other) => false;
            public class Inner;
        }

        public record Note(Pen Pen);

        public enum Color { Red }

        public delegate void Painted(Color color);

        public class Taker : IComparable<Pen[]> {
            public int CompareTo(Pen[]? other) => 0;
        }

        public struct Money {
            public static implicit operator Money(int value) => default;
        }

        public class Bag : List<int> {
            public void Add(string text) { }
        }

        public static class Uses {
            public static void Run() {
                var pen = Make();
                new Circle<IShape>().Draw(pen);
                Base.Inner inner = new();
                _ = typeof(Note);
                _ = nameof(Painted);
                _ = Color.Red;
                foreach (var shape in Shapes()) { }
                Money money = 3;
                var bag = new Bag { 1 };
                var arrays = new[] { Make() };
            }

            static Pen Make() => new();

            static IEnumerable<IShape> Shapes() => [];
        }
        """;

    [Test]
    public void The_facts_have_the_content_of_a_type() {
        var circle = Facts("Circle`1");

        Assert.Multiple(() => {
            Assert.That(circle.Name, Is.EqualTo(new TypeName("Sample.Circle`1", "Sample")));
            Assert.That(circle.Location, Is.EqualTo(new TypeLocation("Sample", 11, 29)));
            Assert.That(circle.Content.Bases.Select(b => b.Name),
                Is.EqualTo(new[] { "Circle`1", "Base", "Object", "IEquatable`1", "IShape" }));
            Assert.That(circle.Content.BaseArguments.Select(b => b.Name), Is.EqualTo(new[] { "Pen" }));
            Assert.That(circle.Content.Attributes.Single().Arguments, Is.EqualTo(new[] { "\"old\"", "false" }));
            Assert.That(circle.Content.TypeParameters.Single().Limits, Is.EqualTo(new[] { "class" }));
            Assert.That((circle.Content.IsSealed, circle.Content.IsPartial), Is.EqualTo((true, true)));
        });
    }

    [Test]
    public void The_facts_have_the_signature_of_each_member() {
        var members = Facts("Circle`1").Content.Members.ToDictionary(m => m.Name);

        Assert.Multiple(() => {
            Assert.That(members.Keys, Is.EqualTo(new[] { "Sides", "Pens", "Draw", "First" }));
            Assert.That(members["Sides"].IsConst, Is.True);
            Assert.That(members["Pens"].Type!.Types.Select(t => t.Name), Is.EqualTo(new[] { "List`1", "Pen" }));
            Assert.That((members["Pens"].Access, members["Pens"].Set), Is.EqualTo((MemberAccess.Internal, (MemberAccess?)MemberAccess.Private)));
            Assert.That(members["Draw"].Implements.Single().Name, Is.EqualTo("IShape"));
            Assert.That(members["First"].Parameters.Select(p => p.Passing), Is.EqualTo(new[] { "ref", null }));
        });
    }

    [TestCase("Circle`1", TypeKind.Class)]
    [TestCase("IShape", TypeKind.Interface)]
    [TestCase("Color", TypeKind.Enum)]
    [TestCase("Painted", TypeKind.Delegate)]
    [TestCase("Note", TypeKind.Class)]
    public void The_facts_have_the_kind_of_a_type(string name, TypeKind kind) =>
        Assert.That(Facts(name).Content.Kind, Is.EqualTo(kind));

    [Test]
    public void A_nested_type_names_its_outer_type() =>
        Assert.That(Facts("Inner").Location.DeclaringType, Is.EqualTo("Sample.Base"));

    [TestCase("Pen", ReferenceKind.Inferred)]
    [TestCase("Circle`1", ReferenceKind.Create)]
    [TestCase("Circle`1", ReferenceKind.Call)]
    [TestCase("IShape", ReferenceKind.TypeArgument)]
    [TestCase("Inner", ReferenceKind.Create)]
    [TestCase("Money", ReferenceKind.Call)]
    [TestCase("List`1", ReferenceKind.Call)]
    [TestCase("Note", ReferenceKind.TypeOf)]
    [TestCase("Painted", ReferenceKind.NameOf)]
    [TestCase("Color", ReferenceKind.Member)]
    [TestCase("IShape", ReferenceKind.Inferred)]
    public void The_facts_have_the_kind_of_each_use(string target, ReferenceKind kind) =>
        Assert.That(Facts("Uses").References.Where(r => r.Target.Name == target).Select(r => r.Kind), Has.Member(kind));

    [Test]
    public void A_base_argument_of_an_array_is_its_element_type() =>
        Assert.That(Facts("Taker").Content.BaseArguments.Select(a => a.Name), Is.EqualTo(new[] { "Pen" }));

    [TestCase("class S { const int A = 1; static readonly int B = 1; readonly int c; int D { get; init; } int E => 1; }", "")]
    [TestCase("class S { int a; static int b; int C { get; set; } int D { get; private set; } int this[int i] { set { } } }", "a,b,C,D,Item")]
    [TestCase("class S { event System.Action E; event System.Action F { add { } remove { } } }", "E")]
    [TestCase("record S(int P);", "")]
    [TestCase("record struct S(int P);", "P")]
    [TestCase("readonly record struct S(int P);", "")]
    [TestCase("interface S { int P { get; set; } }", "P")]
    [TestCase("enum S { A }", "")]
    [TestCase("class S(int p) { int P => p; }", "")]
    [TestCase("class S { int P { get; } = 1; }", "")]
    public void The_facts_have_the_mutable_members(string type, string members) {
        using var build = new SampleBuild();
        build.Write("Ui/S.cs", $"namespace Sample;\npublic {type}\n");
        var snapshot = new ProjectFacts(build.Compile("Sample", "Ui/S.cs"), build.Block).Full();

        Assert.That(string.Join(",", snapshot.Files.Single().Types.Single().Content.MutableMembers), Is.EqualTo(members));
    }

    [Test]
    public void A_qualifier_of_a_nested_type_is_not_a_use() =>
        Assert.That(Facts("Uses").References.Select(r => r.Target.Name), Has.No.Member("Base"));

    [Test]
    public void A_use_has_its_member_and_its_position() {
        var use = Facts("Uses").References.First(r => r is { Kind: ReferenceKind.TypeOf });

        Assert.That((use.Member, use.Line, use.Column), Is.EqualTo(("Run", 46, 20)));
    }

    [Test]
    public void A_snapshot_is_the_same_text_after_a_read() {
        using var build = new SampleBuild();
        build.Write("Ui/All.cs", Source);
        var snapshot = new ProjectFacts(build.Compile("Sample", "Ui/All.cs"), build.Block).Full();

        Assert.That(SnapshotText.Read(snapshot.Write()).Write(), Is.EqualTo(snapshot.Write()));
    }

    static TypeFacts Facts(string name) {
        using var build = new SampleBuild();
        build.Write("Ui/All.cs", Source);
        var snapshot = new ProjectFacts(build.Compile("Sample", "Ui/All.cs"), build.Block).Full();
        return snapshot.Files.Single().Types.Single(t => t.Name.Name == name);
    }
}
