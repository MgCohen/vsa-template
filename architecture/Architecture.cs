namespace Umpire.Architecture;

[Umpire.Markers.RequireDerived]
public abstract class Architecture {
    public virtual IReadOnlyList<FolderAlias> FolderAliases(string blockFolder) => [];

    protected internal virtual void Configure(Options defaults) { }

    protected internal abstract void Define(Block block);

    protected internal virtual void Rules(RuleBook rules) { }
}
