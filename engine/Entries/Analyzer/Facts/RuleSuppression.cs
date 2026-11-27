namespace UmpireEngine;

using Microsoft.CodeAnalysis;

static class RuleSuppression {
    public static bool IsOff(string id, Compilation compilation, CancellationToken cancellation) {
        var provider = compilation.Options.SyntaxTreeOptionsProvider;
        var project = provider?.TryGetGlobalDiagnosticValue(id, cancellation, out var global) == true ? global
            : compilation.Options.SpecificDiagnosticOptions.TryGetValue(id, out var specific) ? specific
            : ReportDiagnostic.Default;
        return compilation.SyntaxTrees.All(tree =>
            (provider?.TryGetDiagnosticValue(tree, id, cancellation, out var file) == true ? file : project) == ReportDiagnostic.Suppress);
    }
}
