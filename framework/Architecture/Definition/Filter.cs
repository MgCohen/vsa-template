namespace VSA.Architecture;

internal sealed record Filter(string Kind, Type Target) {
    public override string ToString() => $"{Kind}<{Concept.Display(Target)}>";
}
