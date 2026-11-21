namespace Umpire.Architecture;

internal sealed class Owner(IReadOnlyList<Entry> subjects, Type attribute) {
    public IReadOnlyList<Entry> Subjects { get; } = subjects;
    public Type Attribute { get; } = attribute;

    public override string ToString() => $"{string.Join(" and ", Subjects)} owns {Attribute.Name}";
}
