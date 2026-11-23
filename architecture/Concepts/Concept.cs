namespace Umpire.Architecture;

public abstract record Concept {
    public virtual string Folder => GetType().Name;
    public virtual string? GroupFolder => null;
    public virtual IReadOnlyList<Type> OfTypes => [];
    public virtual string NameStartsWith => "";
    public virtual string NameEndsWith => "";

    protected virtual void Configure(Options defaults) { }

    internal void ConfigureDefaults(Options defaults) => Configure(defaults);

    internal static string Display(Type type) =>
        type.DeclaringType is null ? type.Name : $"{Display(type.DeclaringType)}.{type.Name}";
}
