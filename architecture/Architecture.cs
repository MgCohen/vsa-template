namespace Umpire.Architecture;

public abstract class Architecture {
    public virtual IReadOnlyList<FolderAlias> FolderAliases(string blockFolder) => [];

    protected virtual void Configure(Options defaults) { }

    protected abstract void Define(Block block);

    protected virtual void Rules(RuleBook rules) { }

    protected virtual void DefineKernel(Part kernel) { }

    protected virtual void KernelRules(RuleBook rules) { }

    internal void ConfigureDefaults(Options defaults) => Configure(defaults);

    internal void DefineBlock(Block block) => Define(block);

    internal void AddRules(RuleBook rules) => Rules(rules);

    internal void DefineKernelPart(Part kernel) {
        DefineKernel(kernel);
        foreach (var part in kernel.Walk()) {
            part.ClearTypes();
        }
    }

    internal void AddKernelRules(RuleBook rules) => KernelRules(rules);
}
