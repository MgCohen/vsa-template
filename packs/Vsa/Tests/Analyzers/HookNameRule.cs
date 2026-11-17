namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class HookNameRule : VsaRule {
    const string Suffix = "Hook";

    static readonly string[] HookBases = [
        "Umpire.Vsa.Handlers.Commands.BeforeCommandHook`1",
        "Umpire.Vsa.Handlers.Commands.AfterCommandHook`2",
        "Umpire.Vsa.Handlers.Events.BeforeEventHook`1",
        "Umpire.Vsa.Handlers.Events.AfterEventHook`1",
    ];

    public override int Number => 9;

    protected override string Title => "The name of a hook ends with Hook";

    protected override string Message =>
        "{0} inherits {1}, so it is a hook. Rename it to {2}.";

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
