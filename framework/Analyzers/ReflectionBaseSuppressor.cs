using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReflectionBaseSuppressor : DiagnosticSuppressor {
    public const string AttributeName = "ReflectionBaseAttribute";

    const string Justification =
        "Reflection uses the members of a type that derives from a type or an interface with [ReflectionBase].";

    public static readonly SuppressionDescriptor UnusedMember = new("VSA9001", "IDE0051", Justification);
    public static readonly SuppressionDescriptor UnreadMember = new("VSA9002", "IDE0052", Justification);

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [UnusedMember, UnreadMember];

    public override void ReportSuppressions(SuppressionAnalysisContext context) {
        foreach (var diagnostic in context.ReportedDiagnostics) {
            if (Descriptor(diagnostic.Id) is not { } descriptor || diagnostic.Location.SourceTree is not { } tree) {
                continue;
            }
            var model = context.GetSemanticModel(tree);
            if (HasReflectionBase(Member(model, diagnostic, context.CancellationToken)?.ContainingType)) {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    static SuppressionDescriptor? Descriptor(string id) => id switch {
        "IDE0051" => UnusedMember,
        "IDE0052" => UnreadMember,
        _ => null,
    };

    static ISymbol? Member(SemanticModel model, Diagnostic diagnostic, CancellationToken cancellation) {
        var node = model.SyntaxTree.GetRoot(cancellation).FindNode(diagnostic.Location.SourceSpan);
        return node.AncestorsAndSelf()
            .Select(ancestor => model.GetDeclaredSymbol(ancestor, cancellation))
            .FirstOrDefault(symbol => symbol is not null);
    }

    static bool HasReflectionBase(INamedTypeSymbol? type) {
        for (var current = type; current is not null; current = current.BaseType) {
            if (IsMarked(current) || current.Interfaces.Any(IsMarked)) {
                return true;
            }
        }
        return type is not null && type.AllInterfaces.Any(IsMarked);
    }

    static bool IsMarked(INamedTypeSymbol type) =>
        type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == AttributeName);
}
