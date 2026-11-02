using System.Reflection;
using VSA.Module;

namespace VSA.Testing.Architecture;

// The assemblies of an app, module by module. The rules read only this, so a test can give any assemblies.
public sealed record VsaApp(IReadOnlyList<AppModule> Modules) {
    // The modules of the test run. The host (AddVsa) finds the same modules with ModuleDiscovery.
    // TEntry is a type of the host, so the test project must reference the host.
    public static VsaApp Of<TEntry>() => new([.. ModuleDiscovery.FindRoots().Select(AppModule.Of)]);

    public IEnumerable<Assembly> Assemblies => Modules.SelectMany(m => m.Assemblies);
}

// One module: its module root and the layer projects that the root recorded at build time.
public sealed record AppModule(string Name, Assembly Root, IReadOnlyList<Assembly> Layers) {
    public IEnumerable<Assembly> Assemblies => [Root, .. Layers];

    public static AppModule Of(ModuleRoot root) => new(root.Name, root.Assembly, [.. root.Layers.Select(Assembly.Load)]);

    public static AppModule Of(Assembly root) => Of(ModuleRoot.Of(root));
}
