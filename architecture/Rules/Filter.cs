namespace Umpire.Architecture;

internal sealed record Filter(FilterKind Kind, Type Target) {
    public override string ToString() => $"{Kind}<{Concept.Display(Target)}>";
}
