namespace EngineCore.Tests;

using Umpire.Architecture;

public record ClosedPackage : Concept {
    protected override void Configure(PartOptions defaults) {
        defaults.Boundary = Boundary.Closed;
    }
}

public record OpenConcept : Concept {
    protected override void Configure(PartOptions defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }
}

public record SameConceptConcept : Concept {
    protected override void Configure(PartOptions defaults) {
        defaults.UseSameConcept = true;
    }
}

public sealed class OptionsArchitecture(Boundary? architecture, bool closedConcept, Boundary? part) : Architecture {
    public Permission MayUse() {
        var resolved = new ResolvedArchitecture(this);
        var user = resolved.Parts.Single(p => p.Is(typeof(User)));
        var target = resolved.Parts.Single(p => p.Is(typeof(Target)));
        return resolved.DecisionFor(
            user, new Dictionary<string, string> { [GroupName] = "a" },
            target, new Dictionary<string, string> { [GroupName] = "b" }).Permission;
    }

    string GroupName => closedConcept ? nameof(ClosedPackage) : nameof(Package);

    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.Boundary = architecture;
    }

    protected override void Define(Part block, Part kernel) {
        var group = closedConcept ? Group<ClosedPackage>(block) : Group<Package>(block);
        group.Add<Target>().And<User>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<Target>();
    }

    PartGroup Group<T>(Part block) where T : Concept, new() =>
        part is { } boundary ? block.AddGroup<T>(boundary) : block.AddGroup<T>();
}

public sealed class EmptyArchitecture : Architecture {
    protected override void Define(Part block, Part kernel) { }
}

public sealed class OptionsTests {
    static readonly Dictionary<string, string> NoValues = [];

    [TestCase(null, false, null, Permission.Allow)]
    [TestCase(Boundary.Closed, false, null, Permission.Deny)]
    [TestCase(Boundary.Open, true, null, Permission.Deny)]
    [TestCase(Boundary.Closed, true, Boundary.Open, Permission.Allow)]
    public void Each_value_comes_from_the_part_then_the_concept_then_the_architecture_then_the_built_in_options(
        Boundary? architecture, bool closedConcept, Boundary? part, Permission expected) {
        var definition = new OptionsArchitecture(architecture, closedConcept, part);

        Assert.That(definition.MayUse(), Is.EqualTo(expected));
    }

    [Test]
    public void An_architecture_that_sets_no_value_has_the_built_in_options() {
        var options = new ResolvedArchitecture(new EmptyArchitecture()).Options;
        var builtIn = PartOptions.BuiltIn;

        Assert.That(new object?[] { options.DefaultPermission, options.Boundary, options.UseSameConcept, options.Depth, options.Access, options.Required },
            Is.EqualTo(new object?[] { builtIn.DefaultPermission, builtIn.Boundary, builtIn.UseSameConcept, builtIn.Depth, builtIn.Access, builtIn.Required }));
    }

    [TestCase(typeof(User), typeof(OpenConcept), Permission.Allow)]
    [TestCase(typeof(User), typeof(Target), Permission.Deny)]
    [TestCase(typeof(OpenConcept), typeof(Target), Permission.Deny)]
    public void The_default_permission_of_a_use_comes_from_the_target(Type user, Type target, Permission expected) {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(block => {
            block.Add<User>();
            block.Add<OpenConcept>();
            block.Add<Target>();
        }));
        var from = resolved.Parts.Single(p => p.Is(user));
        var to = resolved.Parts.Single(p => p.Is(target));

        var decision = resolved.DecisionFor(from, NoValues, to, NoValues);

        Assert.That(decision.Rule, Is.Null);
        Assert.That(decision.Permission, Is.EqualTo(expected));
    }

    [TestCase(typeof(SameConceptConcept), Permission.Allow)]
    [TestCase(typeof(Target), Permission.Deny)]
    public void A_concept_that_sets_UseSameConcept_lets_its_parts_use_each_other(Type concept, Permission expected) {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(block => block.AddGroup<Package>().Add<SameConceptConcept>().And<Target>()));
        var part = resolved.Parts.Single(p => p.Is(concept));

        var decision = resolved.DecisionFor(part, Package("a"), part, Package("b"));

        Assert.That(decision.Permission, Is.EqualTo(expected));
    }

    static Dictionary<string, string> Package(string name) => new() { [nameof(EngineCore.Tests.Package)] = name };
}
