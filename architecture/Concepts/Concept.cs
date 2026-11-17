namespace Umpire.Architecture;

public abstract record Concept {
    public virtual string Folder => GetType().Name;
    public virtual string? GroupFolder => null;

    protected internal virtual void Configure(Options defaults) { }

    protected internal virtual void Define(Part part) { }

    internal static string Display(Type type) =>
        type.DeclaringType is null ? type.Name : $"{Display(type.DeclaringType)}.{type.Name}";
}
