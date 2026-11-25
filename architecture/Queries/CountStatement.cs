namespace Umpire.Architecture;

internal sealed record CountStatement(
    string Rule,
    IReadOnlyList<Part> Subjects,
    bool WholeApp,
    IReadOnlyList<Part>? CountedParts,
    Type? CountedType,
    string Counted,
    int? Min,
    int? Max,
    bool OneKind,
    bool PerPart = false,
    bool SkipTaken = false) {
    public bool Counts(Binding binding) =>
        CountedParts?.Contains(binding.Part) != false && (CountedType is null || binding.Type.Is(CountedType));
}
