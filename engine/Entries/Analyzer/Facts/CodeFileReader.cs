namespace UmpireEngine;

using Facts.Domain.Shared.Compile;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

static class CodeFileReader {
    const string CommentKind = "CommentTrivia";

    static readonly SyntaxKind[] OrderedTypes = [
        SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
        SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration,
    ];

    static readonly SyntaxKind[] Methods = [
        SyntaxKind.MethodDeclaration, SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
        SyntaxKind.OperatorDeclaration, SyntaxKind.ConversionOperatorDeclaration, SyntaxKind.LocalFunctionStatement,
        SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration, SyntaxKind.InitAccessorDeclaration,
        SyntaxKind.AddAccessorDeclaration, SyntaxKind.RemoveAccessorDeclaration,
    ];

    public static CodeFile Read(SemanticModel model, CancellationToken cancellation) {
        var tree = model.SyntaxTree;
        var root = tree.GetRoot(cancellation);
        var file = new SourceFile(tree.FilePath, tree.GetText(cancellation));
        var nodes = root.DescendantNodes().ToList();
        return new CodeFile(
            file.Path,
            [.. file.Text.Lines.Select(line => line.Span.Length)],
            [.. Comments(root, file)],
            [.. TopLevel(root).Select(type => file.Name(Identifier(type)))],
            [.. nodes.OfType<TypeDeclarationSyntax>().Where(type => OrderedTypes.Contains(type.Kind())).Select(type => Body(type, file))],
            [.. nodes.Where(node => Methods.Contains(node.Kind())).Select(node => Method(node, file))],
            [.. nodes.OfType<InvocationExpressionSyntax>().Select(call => BaseCall(call, model, file)).OfType<CodeBaseCall>()]);
    }

    static IEnumerable<CodeComment> Comments(SyntaxNode root, SourceFile file) =>
        root.DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.Kind().ToString().EndsWith(CommentKind, StringComparison.Ordinal))
            .Select(trivia => new CodeComment(trivia.Kind().ToString(), file.Place(SpanWithoutLineBreak(trivia))));

    static TextSpan SpanWithoutLineBreak(SyntaxTrivia trivia) {
        var text = trivia.ToFullString();
        return new TextSpan(trivia.FullSpan.Start, text.TrimEnd('\r', '\n').Length);
    }

    static IEnumerable<MemberDeclarationSyntax> TopLevel(SyntaxNode node) =>
        node.ChildNodes().OfType<MemberDeclarationSyntax>().SelectMany(member => member switch {
            BaseNamespaceDeclarationSyntax space => TopLevel(space),
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => [member],
            _ => Enumerable.Empty<MemberDeclarationSyntax>(),
        });

    static SyntaxToken Identifier(MemberDeclarationSyntax type) => type switch {
        BaseTypeDeclarationSyntax declared => declared.Identifier,
        DelegateDeclarationSyntax declared => declared.Identifier,
        _ => default,
    };

    static CodeTypeBody Body(TypeDeclarationSyntax type, SourceFile file) {
        var inInterface = type is InterfaceDeclarationSyntax;
        return new CodeTypeBody([.. type.Members.Select(member => MemberOf(member, inInterface, file)).OfType<CodeMember>()]);
    }

    static CodeMember? MemberOf(MemberDeclarationSyntax member, bool inInterface, SourceFile file) {
        string? kind = member switch {
            FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ConstKeyword) ? "constant" : "field",
            ConstructorDeclarationSyntax => "constructor",
            DestructorDeclarationSyntax => "finalizer",
            EventFieldDeclarationSyntax or EventDeclarationSyntax => "event",
            BasePropertyDeclarationSyntax => "property",
            BaseMethodDeclarationSyntax => "method",
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => "nested_type",
            _ => null,
        };
        if (kind is null) {
            return null;
        }
        var isStatic = member.Modifiers.Any(SyntaxKind.StaticKeyword) || member.Modifiers.Any(SyntaxKind.ConstKeyword);
        return new CodeMember(kind, AccessOf(member, inInterface), isStatic, file.Name(NameOf(member)));
    }

    static SyntaxToken NameOf(MemberDeclarationSyntax member) => member switch {
        FieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier,
        EventFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier,
        ConstructorDeclarationSyntax constructor => constructor.Identifier,
        DestructorDeclarationSyntax finalizer => finalizer.Identifier,
        PropertyDeclarationSyntax property => property.Identifier,
        IndexerDeclarationSyntax indexer => indexer.ThisKeyword,
        EventDeclarationSyntax declared => declared.Identifier,
        MethodDeclarationSyntax method => method.Identifier,
        OperatorDeclarationSyntax op => op.OperatorToken,
        ConversionOperatorDeclarationSyntax conversion => conversion.OperatorKeyword,
        BaseTypeDeclarationSyntax nested => nested.Identifier,
        DelegateDeclarationSyntax declared => declared.Identifier,
        _ => member.GetFirstToken(),
    };

    static string AccessOf(MemberDeclarationSyntax member, bool inInterface) {
        var modifiers = member.Modifiers;
        var isProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
        if (modifiers.Any(SyntaxKind.PublicKeyword) || IsExplicitImplementation(member)) {
            return "public";
        }
        if (modifiers.Any(SyntaxKind.InternalKeyword)) {
            return isProtected ? "protected_internal" : "internal";
        }
        if (isProtected) {
            return modifiers.Any(SyntaxKind.PrivateKeyword) ? "private_protected" : "protected";
        }
        return inInterface && !modifiers.Any(SyntaxKind.PrivateKeyword) ? "public" : "private";
    }

    static bool IsExplicitImplementation(MemberDeclarationSyntax member) => member switch {
        MethodDeclarationSyntax method => method.ExplicitInterfaceSpecifier is not null,
        BasePropertyDeclarationSyntax property => property.ExplicitInterfaceSpecifier is not null,
        _ => false,
    };

    static CodeMethod Method(SyntaxNode node, SourceFile file) {
        var span = node.GetLocation().GetLineSpan();
        var lines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        var named = node switch {
            MethodDeclarationSyntax method => file.Name(method.Identifier),
            ConstructorDeclarationSyntax constructor => file.Name($"Constructor {constructor.Identifier.Text}", constructor.Identifier.Span),
            DestructorDeclarationSyntax finalizer => file.Name($"Finalizer {finalizer.Identifier.Text}", finalizer.Identifier.Span),
            OperatorDeclarationSyntax op => file.Name($"Operator {op.OperatorToken.Text}", op.OperatorToken.Span),
            ConversionOperatorDeclarationSyntax conversion => file.Name($"Conversion to {conversion.Type}", conversion.Type.Span),
            LocalFunctionStatementSyntax local => file.Name(local.Identifier),
            AccessorDeclarationSyntax accessor => file.Name($"Accessor {accessor.Keyword.Text}", accessor.Keyword.Span),
            _ => file.Name(node.Kind().ToString(), node.Span),
        };
        return new CodeMethod(named, lines);
    }

    static CodeBaseCall? BaseCall(InvocationExpressionSyntax invocation, SemanticModel model, SourceFile file) {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax, Name: var name }
            || invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault() is not { } declaration
            || model.GetDeclaredSymbol(declaration) is not IMethodSymbol method) {
            return null;
        }
        var bases = new List<string>();
        for (var level = method.ContainingType.BaseType; level is not null; level = level.BaseType) {
            bases.Add(level.ToDisplayString());
        }
        return new CodeBaseCall(
            method.ContainingType.Name, method.Name, name.Identifier.Text, method.IsOverride, bases, file.Place(invocation.Span));
    }

    sealed class SourceFile(string path, SourceText text) {
        public string Path { get; } = path;

        public SourceText Text { get; } = text;

        public CodePlace Place(TextSpan span) => CodePlaces.Of(Text, Path, span);

        public CodeName Name(SyntaxToken token) => Name(token.Text, token.Span);

        public CodeName Name(string name, TextSpan span) => new(name, Place(span));
    }
}
