using VSA.Analyzers.Markers;

namespace VSA.Module;

// One class at the root of each module. The host calls Configure at start-up.
[RequireDerived]
public abstract class Module {
    public string Name => ModuleRoot.NameOf(GetType());

    public abstract void Configure(ModuleBuilder module);
}
