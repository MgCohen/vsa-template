namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class SealedRecordRule : VsaRule {
    public const string CheckResponsesKey = "check_responses";

    public override int Number => 1;

    public override IReadOnlyList<UmpireOption> Options { get; } = [UmpireOption.Bool(CheckResponsesKey, true)];

    protected override string Title => "Requests and responses are sealed records";

    protected override string Message =>
        "{0} is {1}. Make it a sealed record.";

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (start.Compilation.GetTypeByMetadataName("Umpire.Vsa.Contracts.Requests.IRequest`1") is not { } request) {
            return;
        }
        start.RegisterSymbolAction(symbol => Check((INamedTypeSymbol)symbol.Symbol, request, symbol), SymbolKind.NamedType);
    }

    void Check(INamedTypeSymbol type, INamedTypeSymbol request, SymbolAnalysisContext context) {
        if (type.IsAbstract || (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)) {
            return;
        }
        var implemented = type.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, request));
        if (implemented is null) {
            return;
        }
        Report(type, "a request", context);
        if (Values(context.Options).Bool(CheckResponsesKey) != true) {
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
