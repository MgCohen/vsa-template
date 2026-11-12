namespace VSA.Architecture;

public abstract record Layer {
    public virtual string Folder => GetType().Name;
    public virtual string? GroupFolder => null;

    protected internal virtual void Define(Part part) { }

    protected internal virtual void Rules(LayerRules rules) { }

    internal static string Display(Type type) =>
        type.DeclaringType is null ? type.Name : $"{Display(type.DeclaringType)}.{type.Name}";
}
