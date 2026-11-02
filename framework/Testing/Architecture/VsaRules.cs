using ArchUnitNET.Fluent;
using VSA.Testing.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VSA.Testing.Architecture;

// The VSA architecture rules as ArchUnitNET rules. VsaArchitectureTests checks each one. A fixture test can check one alone.
public static class VsaRules {
    // VSA2001: each module project uses only the VSA bases and module projects that the layer map permits.
    // Null if no project has a forbidden assembly to check.
    public static IArchRule? LayerMap(VsaApp app, IReadOnlyList<Layer>? layers = null) =>
        LayerRules.For(app, layers).Aggregate((IArchRule?)null, (all, rule) => all is null ? rule : all.And(rule));

    // VSA2002: the host finds the Module class at start-up. Public sealed: no second module class inherits it.
    public static IArchRule ModuleClasses { get; } =
        Classes().That().AreAssignableTo(typeof(VSA.Module.Module)).And().AreNotAbstract()
            .Should().BePublic().AndShould().BeSealed()
            .Because("the host creates each Module class, and no other class may inherit it")
            .WithoutRequiringPositiveResults();
}
