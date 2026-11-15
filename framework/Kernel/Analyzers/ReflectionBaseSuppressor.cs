using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Umpire.Vsa.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReflectionBaseSuppressor : DiagnosticSuppressor {
    public const string AttributeName = "ReflectionBaseAttribute";

    const string Justification =
        "Reflection uses the members of a type that derives from a type or an interface with [ReflectionBase].";

    public static readonly SuppressionDescriptor UnusedMember = new("UMP9001", "IDE0051", Justification);
    public static readonly SuppressionDescriptor UnreadMember = new("UMP9002", "IDE0052", Justification);
    public static readonly SuppressionDescriptor FieldNeverAssigned = new("UMP9003", "CS0649", Justification);
    public static readonly SuppressionDescriptor FieldNeverUsed = new("UMP9004", "CS0169", Justification);
    public static readonly SuppressionDescriptor UnusedParameter = new("UMP9005", "IDE0060", Justification);
    public static readonly SuppressionDescriptor MemberCanBeStatic = new("UMP9006", "CA1822", Justification);
    public static readonly SuppressionDescriptor TypeNeverCreated = new("UMP9007", "CA1812", Justification);

    static readonly ImmutableDictionary<string, SuppressionDescriptor> ById =
        new[] { UnusedMember, UnreadMember, FieldNeverAssigned, FieldNeverUsed, UnusedParameter, MemberCanBeStatic, TypeNeverCreated }
            .ToImmutableDictionary(descriptor => descriptor.SuppressedDiagnosticId);

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [.. ById.Values];

    public override void ReportSuppressions(SuppressionAnalysisContext context) {
        foreach (var diagnostic in context.ReportedDiagnostics) {
            if (!ById.TryGetValue(diagnostic.Id, out var descriptor) || diagnostic.Location.SourceTree is not { } tree) {
                continue;
            }
            var model = context.GetSemanticModel(tree);
            var symbol = Symbol(model, diagnostic, context.CancellationToken);
            if (HasReflectionBase(symbol as INamedTypeSymbol) || HasReflectionBase(symbol?.ContainingType)) {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    static ISymbol? Symbol(SemanticModel model, Diagnostic diagnostic, CancellationToken cancellation) {
        var node = model.SyntaxTree.GetRoot(cancellation).FindNode(diagnostic.Location.SourceSpan);
        return node.AncestorsAndSelf()
            .Select(ancestor => model.GetDeclaredSymbol(ancestor, cancellation))
            .FirstOrDefault(symbol => symbol is not null);
    }

    static bool HasReflectionBase(INamedTypeSymbol? type) {
        if (type is null) {
            return false;
        }
        for (var current = type; current is not null; current = current.BaseType) {
            if (IsMarked(current)) {
                return true;
            }
        }
        return type.AllInterfaces.Any(IsMarked);
    }

    static bool IsMarked(INamedTypeSymbol type) =>
        type.OriginalDefinition.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == AttributeName);
}
