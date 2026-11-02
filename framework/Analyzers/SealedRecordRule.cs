using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1001: requests and responses are sealed records. A record compares by value, and sealed stops a second
// request type that inherits the first and gets its route. The response is checked when it is in the same project.
// Option: vsa.VSA1001.check_responses = false checks only the requests.
public sealed class SealedRecordRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1001",
        "Requests and responses are sealed records",
        "{0} is {1}. Make it a sealed record.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (start.Compilation.GetTypeByMetadataName("VSA.Contracts.Requests.IRequest`1") is not { } request) {
            return;
        }
        start.RegisterSymbolAction(symbol => Check((INamedTypeSymbol)symbol.Symbol, request, symbol), SymbolKind.NamedType);
    }

    void Check(INamedTypeSymbol type, INamedTypeSymbol request, SymbolAnalysisContext context) {
        if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) {
            return;
        }
        var implemented = type.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, request));
        if (implemented is null) {
            return;
        }
        Report(type, "a request", context);
        if (!Options(context.Options, type.Locations[0].SourceTree!).Get("check_responses", fallback: true)) {
            return;
        }
        if (implemented.TypeArguments[0] is INamedTypeSymbol response && response.Locations.Any(l => l.IsInSource)
            && SymbolEqualityComparer.Default.Equals(response.ContainingAssembly, context.Compilation.Assembly)) {
            Report(response, $"the response of {type.Name}", context);
        }
    }

    void Report(INamedTypeSymbol type, string role, SymbolAnalysisContext context) {
        if (type.IsRecord && (type.IsSealed || type.IsValueType)) {
            return;
        }
        context.ReportDiagnostic(Create(type.Locations[0], type.Name, role));
    }
}
