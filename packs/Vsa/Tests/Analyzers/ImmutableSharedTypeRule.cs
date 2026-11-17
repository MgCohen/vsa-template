namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class ImmutableSharedTypeRule : VsaRule {
    public override int Number => 5;

    protected override string Title => "Domain.Shared types are immutable";

    protected override string Message =>
        "{0}.{1} is {2}. A type in a Domain.Shared project does not change after it is made. {3}.";

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (!DomainShared.Is(start.Compilation, start.Options)) {
            return;
        }
        start.RegisterSymbolAction(Check, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);
    }

    void Check(SymbolAnalysisContext context) {
        var member = context.Symbol;
        if (member.IsImplicitlyDeclared || member.Locations.Length == 0 || !member.Locations[0].IsInSource) {
            return;
        }
        var (problem, fix) = member switch {
            IPropertySymbol { SetMethod: { IsInitOnly: false }, ContainingType: { IsRecord: true, IsValueType: true, IsReadOnly: false } } property
                when property.Locations[0].SourceTree?.GetRoot().FindNode(property.Locations[0].SourceSpan) is Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax =>
                ("a property of a record struct that is not readonly", "Make it a readonly record struct"),
            IPropertySymbol { SetMethod: { IsInitOnly: false } } => ("a property with a set accessor", "Use init in place of set"),
            IEventSymbol { AddMethod.IsImplicitlyDeclared: true } => ("a field event", "Remove the event: a Domain.Shared type holds only data"),
            IFieldSymbol { IsReadOnly: false, IsConst: false } => ("a field that is not readonly", "Make the field readonly"),
            _ => (null, null),
        };
        if (problem is not null) {
            context.ReportDiagnostic(Create(member.Locations[0], member.ContainingType.Name, DomainShared.Name(member), problem, fix!));
        }
    }
}
