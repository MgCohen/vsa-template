namespace EngineCore.Tests;

using Umpire.Architecture;

public record ClosedPackage : Concept {
    protected override void Configure(Options defaults) {
        defaults.Boundary = Boundary.Closed;
    }
}

public record DenyingConcept : Concept {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Deny;
    }
}

public record SameConceptConcept : Concept {
    protected override void Configure(Options defaults) {
        defaults.UseSameConcept = true;
    }
}

public sealed class OptionsArchitecture(Boundary? architecture, bool closedConcept, Boundary? part) : Architecture {
    public Permission Decide() {
        var user = Parts().Single(p => p.Is(typeof(User)));
        var target = Parts().Single(p => p.Is(typeof(Target)));
        return Umpire.Architecture.Decide.For(this,
            user, new Dictionary<string, string> { [GroupName] = "a" },
            target, new Dictionary<string, string> { [GroupName] = "b" }).Permission;
    }

    string GroupName => closedConcept ? nameof(ClosedPackage) : nameof(Package);

    protected override void Configure(Options defaults) {
        defaults.Boundary = architecture;
    }

    protected override void Define(Root root) {
        var group = closedConcept ? Group<ClosedPackage>(root) : Group<Package>(root);
        group.Add<Target>().And<User>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<Target>();
    }

    PartGroup Group<T>(Root root) where T : Concept, new() =>
        part is { } boundary ? root.AddGroup<T>(boundary) : root.AddGroup<T>();
}

public sealed class EmptyArchitecture : Architecture {
    protected override void Define(Root root) { }
}

public sealed class OptionsTests {
    [TestCase(null, false, null, Permission.Allow)]
    [TestCase(Boundary.Closed, false, null, Permission.Deny)]
    [TestCase(Boundary.Open, true, null, Permission.Deny)]
    [TestCase(Boundary.Closed, true, Boundary.Open, Permission.Allow)]
    public void Each_value_comes_from_the_part_then_the_concept_then_the_architecture_then_the_built_in_options(
        Boundary? architecture, bool closedConcept, Boundary? part, Permission expected) {
        var definition = new OptionsArchitecture(architecture, closedConcept, part);

        Assert.That(definition.Decide(), Is.EqualTo(expected));
    }

    [Test]
    public void An_architecture_that_sets_no_value_has_the_built_in_options() {
        var options = new EmptyArchitecture().Options;
        var builtIn = Options.BuiltIn;

        Assert.That(new object?[] { options.DefaultPermission, options.Boundary, options.UseSameConcept, options.Depth, options.Access },
            Is.EqualTo(new object?[] { builtIn.DefaultPermission, builtIn.Boundary, builtIn.UseSameConcept, builtIn.Depth, builtIn.Access }));
    }

    [TestCase(typeof(DenyingConcept), nameof(Options.DefaultPermission))]
    [TestCase(typeof(SameConceptConcept), nameof(Options.UseSameConcept))]
    public void A_concept_that_sets_an_option_of_the_architecture_stops_the_definition(Type concept, string option) {
        var definition = new InlineArchitecture(root => {
            if (concept == typeof(DenyingConcept)) {
                root.Add<DenyingConcept>();
            } else {
                root.Add<SameConceptConcept>();
            }
        });

        Assert.That(() => definition.Root, Throws.InvalidOperationException.With.Message.EqualTo(
            $"Define: the concept {concept.Name} sets {option}. Only the architecture sets {option}. Move it to Configure of the architecture."));
    }
}
