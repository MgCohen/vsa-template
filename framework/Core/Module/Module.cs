using VSA.Analyzers.Markers;

namespace VSA.Module;

// One class at the root of each module. The host calls Configure at start-up.
[RequireDerived]
public abstract class Module {
    // The name of the module root assembly, for example "Notes". The layer rules make the folder,
    // the project and the assembly names the same, so the name comes from the assembly.
    public string Name => GetType().Assembly.GetName().Name!;

    public abstract void Configure(ModuleBuilder module);
}
