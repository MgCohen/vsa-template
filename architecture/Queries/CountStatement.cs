namespace Umpire.Architecture;

internal sealed record CountStatement(
    string Rule,
    IReadOnlyList<Part> Subjects,
    bool WholeApp,
    Func<Binding, bool> Counts,
    string Counted,
    int? Min,
    int? Max,
    bool OneKind,
    bool PerPart = false);
