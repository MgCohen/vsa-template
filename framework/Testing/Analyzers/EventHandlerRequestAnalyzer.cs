using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace VSA.Analyzers;

// VSA1002: event handlers do not send requests. An event handler reacts to a fact. It does not use a request type
// or IBus. VSA.Sdk gives the Handlers/Events layer no reference to Contracts/Requests, so this rule catches the rest.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventHandlerRequestAnalyzer() : VsaAnalyzer(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1002",
        "Event handlers do not send requests",
        "Event handler {0} uses {1}. An event handler does not send requests. Move the work to a request handler, or publish an event.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected override void Register(AnalysisContext context) {
        context.RegisterCompilationStartAction(start => {
            var handler = start.Compilation.GetTypeByMetadataName("VSA.Handlers.Events.IntegrationEventHandler`1");
            var routed = start.Compilation.GetTypeByMetadataName("VSA.Contracts.Requests.IRoutedRequest");
            var bus = start.Compilation.GetTypeByMetadataName("VSA.Presenters.IBus");
            if (handler is null || (routed is null && bus is null)) {
                return;
            }
            var forbidden = new Forbidden(routed, bus);
            start.RegisterSymbolStartAction(symbolStart => {
                var type = (INamedTypeSymbol)symbolStart.Symbol;
                if (!Inherits(type, handler)) {
                    return;
                }
                CheckMembers(type, forbidden, symbolStart);
                symbolStart.RegisterOperationAction(op => CheckOperation(type, op, forbidden),
                    OperationKind.ObjectCreation, OperationKind.Invocation, OperationKind.LocalReference);
            }, SymbolKind.NamedType);
        });
    }

    // Fields, properties and parameters, also the parameters of a primary constructor.
    void CheckMembers(INamedTypeSymbol type, Forbidden forbidden, SymbolStartAnalysisContext context) {
        foreach (var member in type.GetMembers()) {
            var types = member switch {
                IFieldSymbol f => new[] { (f.Type, f.Locations) },
                IPropertySymbol p => new[] { (p.Type, p.Locations) },
                IMethodSymbol m => m.Parameters.Select(p => (p.Type, p.Locations)).ToArray(),
                _ => new (ITypeSymbol, ImmutableArray<Location>)[0],
            };
            foreach (var (memberType, locations) in types) {
                if (forbidden.Find(memberType) is { } found && locations.FirstOrDefault(l => l.IsInSource) is { } location) {
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

    sealed class Forbidden(INamedTypeSymbol? routed, INamedTypeSymbol? bus) {
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
            Same(type, bus) || Same(type, routed)
            || type.AllInterfaces.Any(i => Same(i, routed) || Same(i, bus));

        static bool Same(ITypeSymbol a, ITypeSymbol? b) => b is not null && SymbolEqualityComparer.Default.Equals(a.OriginalDefinition, b);
    }
}
