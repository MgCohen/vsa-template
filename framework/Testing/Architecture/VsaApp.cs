using System.Reflection;

namespace VSA.Testing.Architecture;

// The assemblies of an app, module by module. The rules read only this, so a test can give any assemblies.
public sealed record VsaApp(IReadOnlyList<AppModule> Modules) {
    // The modules of the app that contains TEntry, from the project libraries of the test run.
    public static VsaApp Of<TEntry>() => Of(typeof(TEntry).Assembly);

    public static VsaApp Of(Assembly entry) => new([.. ModuleDiscovery.ModuleRoots(entry).Select(AppModule.FromRoot)]);

    public IEnumerable<Assembly> Assemblies => Modules.SelectMany(m => m.Assemblies);
}

// One module: its module root and the layer projects that the root recorded at build time.
public sealed record AppModule(string Name, Assembly Root, IReadOnlyList<Assembly> Layers) {
    public IEnumerable<Assembly> Assemblies => [Root, .. Layers];

    // VSA.Sdk records each layer project of the module root as AssemblyMetadata("VsaLayer", "<assembly name>").
    // A module root with no layers gets one record with an empty value.
    public static AppModule FromRoot(Assembly root) {
        var name = root.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == ModuleDiscovery.ModuleKey)?.Value
            ?? root.GetName().Name!;
        var layers = root.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key == "VsaLayer" && !string.IsNullOrEmpty(a.Value))
            .Select(a => Assembly.Load(a.Value!));
        return new(name, root, [.. layers]);
    }
}
