namespace Umpire.Architecture;

using Umpire.Architecture.Blocks;

public abstract class Architecture {
    internal void ConfigureAll(ArchitectureConfig config) =>
        Levels.Call<Architecture, ArchitectureConfig>(this, nameof(Configure), config);

    internal void DefineRoots(Part root, Part kernel) =>
        Levels.Call<Architecture, Part, Part>(this, nameof(Define), root, kernel);

    internal void DefineKernel(Part kernel) {
        var placers = kernel.Placers;
        kernel.Placers = [GetType()];
        try {
            DefineRoots(new Part(null, "", null) { Placers = [GetType()] }, kernel);
        } finally {
            kernel.Placers = placers;
        }
    }

    internal void AddRules(RuleBook rules) => Levels.Call<Architecture, RuleBook>(this, nameof(Rules), rules);

    protected virtual void Configure(ArchitectureConfig config) {
        config.Roots.Add<App>();
        config.Roots.Add<Definition>();
    }

    protected abstract void Define(Part root, Part kernel);

    protected virtual void Rules(RuleBook rules) { }
}
