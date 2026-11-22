namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public class OwnerArchitecture : Architecture {
    protected override void Define(Part root) {
        root.Add<Nested>();
    }

    public record Nested : Concept;

    public record Free : Concept {
        public override IReadOnlyList<Type>? OwnedBy => [];
    }

    public record Shelf : Block {
        public override IReadOnlyList<Type>? OwnedBy => [];

        protected override void Define(Part part) {
            part.Add<Nested>();
        }
    }
}

public sealed class DerivedOwnerArchitecture : OwnerArchitecture;

public sealed class OtherArchitecture(Action<Part> define) : Architecture {
    protected override void Define(Part root) {
        define(root);
    }
}

public record ByInterface : Concept, IOwnedBy<OwnerArchitecture>;

public record ByRecord : Concept<OwnerArchitecture>;

public record ByList : Concept {
    public override IReadOnlyList<Type>? OwnedBy => [typeof(OwnerArchitecture)];
}

public record ByNoArchitecture : Concept {
    public override IReadOnlyList<Type>? OwnedBy => [typeof(string)];
}

public record Shared : Concept;

public sealed class OwnershipTests {
    [Test]
    public void The_owner_places_a_concept_that_is_nested_in_it() {
        Assert.DoesNotThrow(() => _ = new ResolvedArchitecture(new OwnerArchitecture()));
    }

    [Test]
    public void An_architecture_that_derives_from_the_owner_places_a_nested_concept() {
        Assert.That(new ResolvedArchitecture(new DerivedOwnerArchitecture()).Parts.Select(p => p.Name),
            Is.EqualTo(new[] { "OwnerArchitecture.Nested" }));
    }

    [Test]
    public void Another_architecture_cannot_place_a_nested_concept() {
        var error = Refused(root => root.Add<OwnerArchitecture.Nested>());

        Assert.That(error.Message, Does.Contain("OwnerArchitecture.Nested belongs to OwnerArchitecture"));
    }

    [TestCase(typeof(ByInterface))]
    [TestCase(typeof(ByRecord))]
    [TestCase(typeof(ByList))]
    public void Another_architecture_cannot_place_a_concept_with_a_declared_owner(Type concept) {
        var error = Refused(root => AddOf(root, concept));

        Assert.That(error.Message, Does.Contain("belongs to OwnerArchitecture"));
    }

    [Test]
    public void An_empty_OwnedBy_lets_each_architecture_place_a_nested_concept() {
        Assert.DoesNotThrow(() => _ = new ResolvedArchitecture(new OtherArchitecture(root => root.Add<OwnerArchitecture.Free>())));
    }

    [Test]
    public void A_top_level_concept_with_no_owner_is_shared() {
        Assert.DoesNotThrow(() => _ = new ResolvedArchitecture(new OtherArchitecture(root => root.Add<Shared>())));
    }

    [Test]
    public void A_block_body_adds_the_concepts_of_its_home_in_another_architecture() {
        var resolved = new ResolvedArchitecture(new OtherArchitecture(root => root.Add<OwnerArchitecture.Shelf>()));

        Assert.That(resolved.Parts.Select(p => p.Name), Is.EqualTo(new[] { "OwnerArchitecture.Shelf", "OwnerArchitecture.Nested" }));
    }

    [Test]
    public void An_owner_that_is_not_an_architecture_is_an_error() {
        var error = Refused(root => root.Add<ByNoArchitecture>());

        Assert.That(error.Message, Does.Contain("String is not an Architecture"));
    }

    static InvalidOperationException Refused(Action<Part> define) =>
        Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new OtherArchitecture(define)))!;

    static void AddOf(Part root, Type concept) {
        if (concept == typeof(ByInterface)) {
            root.Add<ByInterface>();
        } else if (concept == typeof(ByRecord)) {
            root.Add<ByRecord>();
        } else {
            root.Add<ByList>();
        }
    }
}
