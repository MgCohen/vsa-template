using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace VSA.Analyzers;

// The base of a rule that forbids some types in the classes that inherit one base class.
// It checks fields, properties, parameters (also of a primary constructor), object creations, calls and locals.
// The message gets two arguments: the class name and the forbidden type name.
public abstract class ForbiddenUseRule(DiagnosticDescriptor rule) : VsaAnalyzerRule(rule) {
    // The base class of the checked classes. Null: the compilation does not have it, and the rule does nothing.
    protected abstract INamedTypeSymbol? Subject(Compilation compilation);

    // The forbidden types: each type, its subtypes and the types that implement it. Null entries are ignored.
    protected abstract IEnumerable<INamedTypeSymbol?> ForbiddenTypes(Compilation compilation);

    protected internal sealed override void Register(CompilationStartAnalysisContext start) {
        var subject = Subject(start.Compilation);
        var forbidden = new Forbidden([.. ForbiddenTypes(start.Compilation).OfType<INamedTypeSymbol>()]);
        if (subject is null || forbidden.IsEmpty) {
            return;
        }
        start.RegisterSymbolStartAction(symbolStart => {
            var type = (INamedTypeSymbol)symbolStart.Symbol;
            if (!Inherits(type, subject)) {
                return;
            }
            CheckMembers(type, forbidden, symbolStart);
            symbolStart.RegisterOperationAction(op => CheckOperation(type, op, forbidden),
                OperationKind.ObjectCreation, OperationKind.Invocation, OperationKind.LocalReference);
        }, SymbolKind.NamedType);
    }

    // Two members can have the same location, for example a primary constructor parameter in the constructor and in
    // another member that the compiler makes from it. The rule reports each location one time.
    void CheckMembers(INamedTypeSymbol type, Forbidden forbidden, SymbolStartAnalysisContext context) {
        var reported = new HashSet<Location>();
        foreach (var member in type.GetMembers()) {
            var types = member switch {
                IFieldSymbol f => new[] { (f.Type, f.Locations) },
                IPropertySymbol p => new[] { (p.Type, p.Locations) },
                IMethodSymbol m => m.Parameters.Select(p => (p.Type, p.Locations)).ToArray(),
                _ => new (ITypeSymbol, ImmutableArray<Location>)[0],
            };
            foreach (var (memberType, locations) in types) {
                if (forbidden.Find(memberType) is { } found && locations.FirstOrDefault(l => l.IsInSource) is { } location && reported.Add(location)) {
                    context.RegisterSymbolEndAction(end => end.ReportDiagnostic(Create(location, type.Name, found.Name)));
                }
            }
        }
    }

    void CheckOperation(INamedTypeSymbol type, OperationAnalysisContext context, Forbidden forbidden) {
        var found = context.Operation switch {
            IObjectCreationOperation create => forbidden.Find(create.Type),
            IInvocationOperation call => forbidden.Find(call.TargetMethod.ContainingType)
                ?? call.TargetMethod.TypeArguments.Select(forbidden.Find).FirstOrDefault(t => t is not null),
            ILocalReferenceOperation local => forbidden.Find(local.Type),
            _ => null,
        };
        if (found is not null) {
            context.ReportDiagnostic(Create(context.Operation.Syntax.GetLocation(), type.Name, found.Name));
        }
    }

    static bool Inherits(INamedTypeSymbol type, INamedTypeSymbol openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (SymbolEqualityComparer.Default.Equals(b.OriginalDefinition, openBase)) {
                return true;
            }
        }
        return false;
    }

    sealed class Forbidden(ImmutableArray<INamedTypeSymbol> types) {
        public bool IsEmpty => types.IsEmpty;

        // The forbidden type in this type, its element type or its type arguments, or null.
        public ITypeSymbol? Find(ITypeSymbol? type) {
            switch (type) {
                case null:
                    return null;
                case IArrayTypeSymbol array:
                    return Find(array.ElementType);
                case INamedTypeSymbol named:
                    if (Is(named)) {
                        return named;
                    }
                    return named.TypeArguments.Select(Find).FirstOrDefault(t => t is not null);
                default:
                    return null;
            }
        }

        bool Is(INamedTypeSymbol type) =>
            types.Any(f => Same(type, f) || Inherits(type, f) || type.AllInterfaces.Any(i => Same(i, f)));

        static bool Same(ITypeSymbol a, ITypeSymbol b) => SymbolEqualityComparer.Default.Equals(a.OriginalDefinition, b);
    }
}
