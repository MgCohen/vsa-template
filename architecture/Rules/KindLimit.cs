namespace Umpire.Architecture;

internal sealed class KindLimit(IReadOnlyList<Entry> subjects, TypeKind kinds) {
    public IReadOnlyList<Entry> Subjects { get; } = subjects;
    public TypeKind Kinds { get; } = kinds;

    public override string ToString() =>
        $"{string.Join(" and ", Subjects)} " + (Kinds == TypeKind.None ? "has no type" : $"has only {KindText.Of(Kinds)}");
}
