using ArchUnitNET.Loader;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Testing.Architecture;

// An optional helper for ArchUnitNET rules. A rule is one [Test] in VsaArchitectureTests.
public static class VsaRules {
    // The module projects and the VSA bases. ArchUnitNET checks only the types that it loads.
    public static ArchModel Load(VsaApp app) => new ArchLoader().LoadAssemblies([.. app.Assemblies, .. LayerRules.Bases()]).Build();
}
