namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// UMP010104: a file has one top-level type. Nested types are not top-level types.
// The diagnostic marks each top-level type after the first.
// Option: umpire.UMP010104.allow_same_name (in Umpire.Rules.globalconfig). If true, types with the name of the first type can share its file,
// for example Module and Module<TSettings>, which have a different number of type parameters.
public sealed class OneTypePerFileRule() : UmpireAnalyzerRule(Descriptor) {
    public const string AllowSameNameKey = "allow_same_name";

    public static readonly DiagnosticDescriptor Descriptor = new(
        "UMP010104",
        "A file has one top-level type",
        "{0} is a second top-level type in the file of {1}. Move {0} to its own file, {0}.cs.",
        "Umpire",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override bool IsModuleRule => false;

    public override IReadOnlyList<UmpireOption> RequiredOptions { get; } = [UmpireOption.Bool(AllowSameNameKey)];

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        var types = TopLevel(context.Tree.GetRoot(context.CancellationToken)).ToList();
        if (types.Count < 2) {
            return;
        }
        if (Options(context.Options, context.Tree).Bool(AllowSameNameKey) is not { } allowSameName) {
            return;
        }
        var first = Identifier(types[0]).Text;
        foreach (var type in types.Skip(1).Where(t => !allowSameName || Identifier(t).Text != first)) {
            var identifier = Identifier(type);
            context.ReportDiagnostic(Create(identifier.GetLocation(), identifier.Text, first));
        }
    }

    // The types of the file and of its namespaces, but not the nested types.
    static IEnumerable<MemberDeclarationSyntax> TopLevel(SyntaxNode node) =>
        node.ChildNodes().OfType<MemberDeclarationSyntax>().SelectMany(member => member switch {
            BaseNamespaceDeclarationSyntax ns => TopLevel(ns),
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => [member],
            _ => Enumerable.Empty<MemberDeclarationSyntax>(),
        });

    static SyntaxToken Identifier(MemberDeclarationSyntax type) => type switch {
        BaseTypeDeclarationSyntax t => t.Identifier,
        DelegateDeclarationSyntax d => d.Identifier,
        _ => default,
    };
}
