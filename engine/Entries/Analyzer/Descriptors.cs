namespace UmpireEngine;

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Operations.Infrastructure;

static class Descriptors {
    const string Category = "Umpire";
    const string Format = "{0}";
    const string LoadTitle = "The Umpire analyzers cannot load a library";
    const string LoadMessage = "The Umpire analyzers cannot load a library: {0} The analyzer DLL embeds Umpire.Config. Use a "
        + "compiler on .NET 8 or later. The Umpire rules do not run until the library loads.";
    const string RunTitle = "An operation of the engine could not run";

    static readonly Lazy<Known> Codes = new(Read, LazyThreadSafetyMode.ExecutionAndPublication);

    public const string LoadCode = "UMP010003";

    public static ImmutableArray<DiagnosticDescriptor> All => Codes.Value.All;

    public static IReadOnlyList<string> CodeRules => Codes.Value.CodeRules;

    public static DiagnosticDescriptor? Of(string code) => Codes.Value.ByCode.TryGetValue(code, out var descriptor) ? descriptor : null;

    public static Diagnostic LoadFailure(string reason) => Diagnostic.Create(
        Codes.Value.ByCode[LoadCode], Location.None, string.Format(CultureInfo.InvariantCulture, LoadMessage, reason));

    public static Diagnostic Failure(string reason, Location? location = null) =>
        Diagnostic.Create(Codes.Value.ByCode[RunCodes.CouldNotRun], location ?? Location.None, reason);

    static Known Read() {
        IReadOnlyList<KnownCode> codes;
        try {
            codes = EngineCodes.Read();
        } catch (Exception e) when (e is not OperationCanceledException) {
            codes = [];
        }
        var byCode = codes.Select(code => Of(code.Code, code.Title, code.AtEnd))
            .Append(Of(LoadCode, LoadTitle, end: true))
            .Append(Of(RunCodes.CouldNotRun, RunTitle, end: false))
            .GroupBy(descriptor => descriptor.Id)
            .ToDictionary(group => group.Key, group => group.First());
        return new Known(byCode, [.. byCode.Values], [.. codes.Where(code => code.IsCodeRule).Select(code => code.Code)]);
    }

    static DiagnosticDescriptor Of(string code, string title, bool end) => end
        ? new(code, title, Format, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, customTags: WellKnownDiagnosticTags.CompilationEnd)
        : new(code, title, Format, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    sealed class Known(Dictionary<string, DiagnosticDescriptor> byCode, ImmutableArray<DiagnosticDescriptor> all, IReadOnlyList<string> codeRules) {
        public Dictionary<string, DiagnosticDescriptor> ByCode { get; } = byCode;

        public ImmutableArray<DiagnosticDescriptor> All { get; } = all;

        public IReadOnlyList<string> CodeRules { get; } = codeRules;
    }
}
