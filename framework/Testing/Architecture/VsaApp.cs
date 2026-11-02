using VSA.Module;

namespace VSA.Testing.Architecture;

// The module roots of an app. The rules read only this, so a test can give any module roots.
public sealed record VsaApp(IReadOnlyList<ModuleRoot> Modules) {
    // The modules of the test run. The host (AddVsa) finds the same modules with ModuleDiscovery.
    public static VsaApp Current() => new(ModuleDiscovery.FindRoots());
}
