using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Testing.Architecture;

// Optional helpers for ArchUnitNET rules. A rule does not have to use them: a rule is one [Test] in VsaArchitectureTests.
public static class VsaRules {
    // The module projects and the VSA bases. ArchUnitNET checks only the types that it loads.
    public static ArchModel Load(VsaApp app) => new ArchLoader().LoadAssemblies([.. app.Assemblies, .. LayerRules.Bases()]).Build();

    // One rule that fails for each failure of the rules. Null if there are no rules.
    public static IArchRule? All(IEnumerable<IArchRule> rules) =>
        rules.Aggregate((IArchRule?)null, (all, rule) => all is null ? rule : all.And(rule));
}
