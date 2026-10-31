namespace VSA.Module;

// One class at the root of each module. The host calls Configure at start-up.
public abstract class Module {
    // The module folder and the prefix of its projects, for example "Notes".
    public abstract string Name { get; }

    public abstract void Configure(ModuleBuilder module);
}
