namespace Umpire.Architecture;

public abstract class Architecture {
    public virtual IReadOnlyList<FolderAlias> FolderAliases(string blockFolder) => [];

    protected virtual Type? PackKernel => null;

    protected virtual void Configure(Options defaults) { }

    protected abstract void Define(Part root);

    protected virtual void Rules(RuleBook rules) { }

    internal Type? KernelOfPack => PackKernel;

    internal void ConfigureDefaults(Options defaults) => Levels.Call<Architecture, Options>(this, nameof(Configure), defaults);

    internal void DefineRoot(Part root) => Levels.Call<Architecture, Part>(this, nameof(Define), root);

    internal void AddRules(RuleBook rules) => Levels.Call<Architecture, RuleBook>(this, nameof(Rules), rules);
}
