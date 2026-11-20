namespace Umpire.Architecture;

public abstract record Concept {
    public virtual string Folder => GetType().Name;
    public virtual string? GroupFolder => null;

    protected virtual void Configure(Options defaults) { }

    protected virtual void Define(Part part) { }

    protected virtual void Rules(RuleBook rules) { }

    internal void ConfigureDefaults(Options defaults) => Configure(defaults);

    internal void DefinePart(Part part) => Define(part);

    internal void AddRules(RuleBook rules) => Rules(rules);

    internal static string Display(Type type) =>
        type.DeclaringType is null ? type.Name : $"{Display(type.DeclaringType)}.{type.Name}";
}
