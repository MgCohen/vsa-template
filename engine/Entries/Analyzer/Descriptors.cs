namespace UmpireEngine;

using System.Collections.Immutable;
using CodeCheck.Slices.CheckCode;
using Microsoft.CodeAnalysis;
using Operations.Infrastructure;
using Umpire.CheckModel;

static class Descriptors {
    const string Category = "Umpire";
    const string Format = "{0}";

    static readonly Dictionary<string, DiagnosticDescriptor> ByCode = CodeRule.All.Select(rule => Of(rule.Id, rule.Title, end: false))
        .Concat(CodeRule.Notices.Select(notice => Of(notice.Id, notice.Title, end: true)))
        .Concat(RuleCodes.All.Select(code => Of(code, "An architecture rule of the definition", end: false)))
        .Concat(new[] { DataCodes.NoModel, DataCodes.NoSnapshot, DataCodes.OldSnapshot, DataCodes.NotSaved }
            .Select(code => Of(code, "The data of the check model", end: true)))
        .Append(Of(RunCodes.CouldNotRun, "An operation of the engine could not run", end: false))
        .ToDictionary(descriptor => descriptor.Id);

    public static ImmutableArray<DiagnosticDescriptor> All { get; } = [.. ByCode.Values];

    public static DiagnosticDescriptor? Of(string code) => ByCode.TryGetValue(code, out var descriptor) ? descriptor : null;

    public static Diagnostic LoadFailure(string reason) =>
        Diagnostic.Create(ByCode[CodeRule.LoadNotice.Id], Location.None, CodeRule.LoadNotice.Text(reason));

    public static Diagnostic Failure(string reason, Location? location = null) =>
        Diagnostic.Create(ByCode[RunCodes.CouldNotRun], location ?? Location.None, reason);

    static DiagnosticDescriptor Of(string code, string title, bool end) => end
        ? new(code, title, Format, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, customTags: WellKnownDiagnosticTags.CompilationEnd)
        : new(code, title, Format, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
