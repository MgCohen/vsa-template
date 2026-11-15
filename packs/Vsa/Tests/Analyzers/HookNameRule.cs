using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

public sealed class HookNameRule() : VsaAnalyzerRule(Descriptor) {
    const string Suffix = "Hook";

    static readonly string[] HookBases = [
        "VSA.Handlers.Commands.BeforeCommandHook`1",
        "VSA.Handlers.Commands.AfterCommandHook`2",
        "VSA.Handlers.Events.BeforeEventHook`1",
        "VSA.Handlers.Events.AfterEventHook`1",
    ];

    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1015",
        "The name of a hook ends with Hook",
        "{0} inherits {1}, so it is a hook. Rename it to {2}.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) {
        List<INamedTypeSymbol> bases =
            [.. HookBases.Select(start.Compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>()];
        if (bases.Count == 0) {
            return;
        }
        start.RegisterSymbolAction(context => {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class || type.Name.EndsWith(Suffix, System.StringComparison.Ordinal)
                || HookBaseOf(type, bases) is not { } hookBase) {
                return;
            }
            context.ReportDiagnostic(Create(type.Locations[0], type.Name, hookBase.Name, type.Name + Suffix));
        }, SymbolKind.NamedType);
    }

    static INamedTypeSymbol? HookBaseOf(INamedTypeSymbol type, List<INamedTypeSymbol> bases) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            var definition = b.OriginalDefinition;
            if (bases.FirstOrDefault(h => SymbolEqualityComparer.Default.Equals(definition, h)) is { } found) {
                return found;
            }
        }
        return null;
    }
}
