using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace VSA.Analyzers;

// VSA1006: the types of a <Module>.Domain.Shared project hold only data. The logic stays in the module that owns it.
// A type has no ordinary method, no operator or conversion written by hand, and no constructor with a body.
// A primary constructor, and a constructor with only a base(...) or this(...) initializer, are permitted.
// A property or indexer body calls no method: "=> A + B" is permitted, "=> Compute()" is not.
// A property initializer can call a method, for example a static key that a factory makes.
// The compiler makes the members of a record, so this rule does not check them. This rule is a trial (decision D-37).
public sealed class DataOnlySharedTypeRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1006",
        "Domain.Shared types hold only data",
        "{0}.{1} is {2}. A type in a Domain.Shared project holds only data. Move the logic to the module that owns it.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (!DomainShared.Is(start.Compilation, start.Options)) {
            return;
        }
        start.RegisterSymbolAction(CheckMethod, SymbolKind.Method);
        // One diagnostic for each property, also when its body has more than one call.
        var reported = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
        start.RegisterOperationAction(context => CheckCall(context, reported), OperationKind.Invocation);
    }

    void CheckMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;
        if (method.IsImplicitlyDeclared || method.ContainingType.TypeKind == TypeKind.Delegate
            || method.Locations.FirstOrDefault(l => l.IsInSource) is not { } location) {
            return;
        }
        var problem = method.MethodKind switch {
            MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.Destructor => "a method",
            MethodKind.UserDefinedOperator => "an operator",
            MethodKind.Conversion => "a conversion",
            MethodKind.Constructor or MethodKind.StaticConstructor when HasBody(method) => "a constructor with a body",
            _ => null,
        };
        if (problem is not null) {
            context.ReportDiagnostic(Create(location, method.ContainingType.Name, DomainShared.Name(method), problem));
        }
    }

    // A primary constructor has no ConstructorDeclarationSyntax. An empty body with an initializer is permitted.
    static bool HasBody(IMethodSymbol constructor) =>
        constructor.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<ConstructorDeclarationSyntax>()
            .Any(c => c.ExpressionBody is not null || c.Body is { Statements.Count: > 0 });

    void CheckCall(OperationAnalysisContext context, ConcurrentDictionary<ISymbol, bool> reported) {
        if (context.ContainingSymbol is not IMethodSymbol { AssociatedSymbol: IPropertySymbol property } accessor
            || accessor.MethodKind is not (MethodKind.PropertyGet or MethodKind.PropertySet)
            || property.IsImplicitlyDeclared || property.Locations.FirstOrDefault(l => l.IsInSource) is not { } location
            || !reported.TryAdd(property, true)) {
            return;
        }
        var kind = property.IsIndexer ? "an indexer" : "a property";
        context.ReportDiagnostic(Create(location, property.ContainingType.Name, DomainShared.Name(property),
            $"{kind} with a body that calls {((IInvocationOperation)context.Operation).TargetMethod.Name}"));
    }
}
