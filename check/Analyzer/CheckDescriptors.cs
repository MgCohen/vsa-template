namespace Umpire.CheckModel.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

static class CheckDescriptors {
    const string Category = "Umpire";

    static readonly Dictionary<string, DiagnosticDescriptor> ByCode = RuleCodes.All.Select(Rule)
        .Concat(new[] { DataCodes.NoModel, DataCodes.NoSnapshot, DataCodes.OldSnapshot, DataCodes.NotSaved }.Select(Data))
        .ToDictionary(d => d.Id);

    public static ImmutableArray<DiagnosticDescriptor> All { get; } = [.. ByCode.Values];

    public static DiagnosticDescriptor Of(string code) => ByCode[code];

    static DiagnosticDescriptor Rule(string code) =>
        new(code, "An architecture rule of the definition", "{0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    static DiagnosticDescriptor Data(string code) => new(
        code, "The data of the check model", "{0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);
}
