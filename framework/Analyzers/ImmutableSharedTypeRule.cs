using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1004: the types of a <Module>.Domain.Shared project are immutable. Other modules read these values,
// so a value does not change after it is made. A property has no set accessor (init is permitted).
// A field is readonly or const. This rule is a trial (decision D-37).
public sealed class ImmutableSharedTypeRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1004",
        "Domain.Shared types are immutable",
        "{0}.{1} is {2}. A type in a Domain.Shared project does not change after it is made. {3}.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (!DomainShared.Is(start.Compilation, start.Options)) {
            return;
        }
        start.RegisterSymbolAction(Check, SymbolKind.Property, SymbolKind.Field);
    }

    void Check(SymbolAnalysisContext context) {
        var member = context.Symbol;
        if (member.IsImplicitlyDeclared || member.Locations.Length == 0 || !member.Locations[0].IsInSource) {
            return;
        }
        var (problem, fix) = member switch {
            IPropertySymbol { SetMethod: { IsInitOnly: false } } => ("a property with a set accessor", "Use init in place of set"),
            IFieldSymbol { IsReadOnly: false, IsConst: false } => ("a field that is not readonly", "Make the field readonly"),
            _ => (null, null),
        };
        if (problem is not null) {
            context.ReportDiagnostic(Create(member.Locations[0], member.ContainingType.Name, DomainShared.Name(member), problem, fix!));
        }
    }
}
