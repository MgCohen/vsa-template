using VSA.Module;

namespace VSA.Testing.Architecture;

// The module roots of an app. The rules read only this, so a test can give any module roots.
public sealed record VsaApp(IReadOnlyList<ModuleRoot> Modules) {
    // The modules of the test run. VsaBuilder finds the same modules with AppScan.
    public static VsaApp Current() => new(AppScan.ModuleRoots());
}
