namespace Umpire.Vsa.Analyzers;

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

public sealed class DataOnlySharedTypeRule : VsaRule {
    public override int Number => 6;

    protected override string Title => "Domain.Shared types hold only data";

    protected override string Message =>
        "{0}.{1} is {2}. A type in a Domain.Shared project holds only data. Move the logic to the module that owns it.";

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (!DomainShared.Is(start.Compilation, start.Options)) {
            return;
        }
        start.RegisterSymbolAction(CheckMethod, SymbolKind.Method);
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
