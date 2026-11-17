namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class OneTypePerFileRule : KernelRule {
    public const string AllowSameNameKey = "allow_same_name";

    public override int Number => 4;

    public override bool IsModuleRule => false;

    public override IReadOnlyList<UmpireOption> Options { get; } = [UmpireOption.Bool(AllowSameNameKey, true)];

    protected override string Title => "A file has one top-level type";

    protected override string Message =>
        "{0} is a second top-level type in the file of {1}. Move {0} to its own file, {0}.cs.";

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        var types = TopLevel(context.Tree.GetRoot(context.CancellationToken)).ToList();
        if (types.Count < 2) {
            return;
        }
        if (Values(context.Options, context.Tree).Bool(AllowSameNameKey) is not { } allowSameName) {
            return;
        }
        var first = Identifier(types[0]).Text;
        foreach (var type in types.Skip(1).Where(t => !allowSameName || Identifier(t).Text != first)) {
            var identifier = Identifier(type);
            context.ReportDiagnostic(Create(identifier.GetLocation(), identifier.Text, first));
        }
    }

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
