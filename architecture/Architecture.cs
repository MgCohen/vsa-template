namespace Umpire.Architecture;

[Umpire.Markers.RequireDerived]
public abstract class Architecture {
    public virtual IReadOnlyList<FolderAlias> FolderAliases(string blockFolder) => [];

    protected virtual void Configure(Options defaults) { }

    protected abstract void Define(Block block);

    protected virtual void Rules(RuleBook rules) { }

    internal void ConfigureDefaults(Options defaults) => Configure(defaults);

    internal void DefineBlock(Block block) => Define(block);

    internal void AddRules(RuleBook rules) => Rules(rules);
}
