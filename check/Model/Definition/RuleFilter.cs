namespace Umpire.CheckModel;

public sealed record RuleFilter(FilterKind Kind, TypeName Concept) {
    public override string ToString() => $"{Kind}<{Concept.Display}>";
}
