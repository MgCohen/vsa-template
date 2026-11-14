namespace VSA.Architecture;

public abstract record Concept {
    internal virtual Type Identity => GetType();

    public virtual string Folder => GetType().Name;
    public virtual string? GroupFolder => null;

    protected internal virtual void Configure(ConceptOptions options) { }

    protected internal virtual void Define(Part part) { }

    protected internal virtual void Rules(ConceptRules rules) { }

    internal ConceptOptions Configured() {
        var options = new ConceptOptions();
        Configure(options);
        return options;
    }

    internal static string Display(Type type) =>
        type.DeclaringType is null ? type.Name : $"{Display(type.DeclaringType)}.{type.Name}";
}
