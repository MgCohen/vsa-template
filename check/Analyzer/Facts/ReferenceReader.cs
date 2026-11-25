namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class ReferenceReader {
    readonly SemanticModel model;
    readonly SourcePlaces places;
    readonly TypeName owner;
    readonly string file;
    readonly Dictionary<Key, AppReference> found = [];
    string? member;

    ReferenceReader(SemanticModel model, SourcePlaces places, TypeName owner, string file) {
        this.model = model;
        this.places = places;
        this.owner = owner;
        this.file = file;
    }

    public static IReadOnlyList<AppReference> Read(INamedTypeSymbol type, SourcePlaces places, Func<SyntaxTree, SemanticModel> models) {
        var name = Symbols.NameOf(type);
        var references = new List<AppReference>();
        foreach (var declaration in type.DeclaringSyntaxReferences.Where(d => places.FileOf(d.SyntaxTree).Length > 0)) {
            var reader = new ReferenceReader(models(declaration.SyntaxTree), places, name, places.FileOf(declaration.SyntaxTree));
            reader.Walk(declaration.GetSyntax());
            references.AddRange(reader.found.Values);
        }
        return references;
    }

    static bool IsType(SyntaxNode node) => node is DelegateDeclarationSyntax or BaseTypeDeclarationSyntax { Identifier.ValueText.Length: > 0 };

    static string? NameOf(SyntaxNode node) => node switch {
        MethodDeclarationSyntax method => method.Identifier.ValueText,
        ConstructorDeclarationSyntax => ".ctor",
        PropertyDeclarationSyntax property => property.Identifier.ValueText,
        EventDeclarationSyntax declared => declared.Identifier.ValueText,
        BaseFieldDeclarationSyntax field => string.Join(",", field.Declaration.Variables.Select(v => v.Identifier.ValueText)),
        IndexerDeclarationSyntax => "this[]",
        OperatorDeclarationSyntax op => "operator " + op.OperatorToken.ValueText,
        ConversionOperatorDeclarationSyntax => "operator",
        DestructorDeclarationSyntax => "Finalize",
        EnumMemberDeclarationSyntax value => value.Identifier.ValueText,
        _ => null,
    };

    void Walk(SyntaxNode root) {
        foreach (var node in root.DescendantNodes(n => n == root || !IsType(n))) {
            if (IsType(node)) {
                continue;
            }
            member = node.AncestorsAndSelf().TakeWhile(n => n != root).Select(NameOf).FirstOrDefault(n => n is not null);
            Visit(node);
        }
    }

    void Visit(SyntaxNode node) {
        switch (node) {
            case SimpleNameSyntax name:
                Name(name);
                break;
            case BaseObjectCreationExpressionSyntax creation when model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor:
                Add(constructor.ContainingType, ReferenceKind.Create, creation, Signature(constructor));
                break;
            case ElementAccessExpressionSyntax or ElementBindingExpressionSyntax or BinaryExpressionSyntax
                or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax or AssignmentExpressionSyntax:
                Used(model.GetSymbolInfo(node).Symbol, node);
                break;
            case ConstructorInitializerSyntax initializer when model.GetSymbolInfo(initializer).Symbol is IMethodSymbol other:
                Add(other.ContainingType, ReferenceKind.Call, initializer, Signature(other));
                break;
            case SingleVariableDesignationSyntax or ParameterSyntax { Type: null }:
                Inferred(model.GetDeclaredSymbol(node), node);
                break;
            case InvocationExpressionSyntax when model.GetSymbolInfo(node).Symbol is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } invoke:
                Add(invoke.ContainingType, ReferenceKind.Call, node, Signature(invoke));
                break;
            case CollectionExpressionSyntax collection:
                AddAll(model.GetTypeInfo(collection).ConvertedType, ReferenceKind.Inferred, node);
                break;
            case InterpolationSyntax interpolation:
                AddAll(model.GetTypeInfo(interpolation.Expression).Type, ReferenceKind.Inferred, node);
                break;
            case ForEachStatementSyntax loop:
                Used(model.GetForEachStatementInfo(loop).GetEnumeratorMethod, loop);
                break;
            case ImplicitArrayCreationExpressionSyntax array:
                AddAll(model.GetTypeInfo(array).Type, ReferenceKind.Inferred, node);
                break;
            case AwaitExpressionSyntax wait:
                Used(model.GetAwaitExpressionInfo(wait).GetAwaiterMethod, wait);
                break;
            case QueryClauseSyntax clause:
                var query = model.GetQueryClauseInfo(clause);
                Used(query.CastInfo.Symbol, clause);
                Used(query.OperationInfo.Symbol, clause);
                break;
            case SelectOrGroupClauseSyntax or OrderingSyntax:
                Used(model.GetSymbolInfo(node).Symbol, node);
                break;
        }
        if (node is AssignmentExpressionSyntax assignment) {
            Used(model.GetDeconstructionInfo(assignment).Method, node);
        }
        if (node.Parent is InitializerExpressionSyntax { RawKind: (int)SyntaxKind.CollectionInitializerExpression }) {
            Used(model.GetCollectionInitializerSymbolInfo((ExpressionSyntax)node).Symbol, node);
        }
        if (node is ExpressionSyntax and not TypeSyntax or IdentifierNameSyntax && model.GetConversion(node) is { IsUserDefined: true } conversion) {
            Used(conversion.MethodSymbol, node);
        }
    }

    void Name(SimpleNameSyntax name) {
        if (name.Identifier.ValueText == "var" && name.IsVar) {
            AddAll(model.GetTypeInfo(name).Type, ReferenceKind.Inferred, name);
            return;
        }
        var info = model.GetSymbolInfo(name);
        var symbol = info.Symbol ?? (IsInNameOf(name) ? info.CandidateSymbols.FirstOrDefault() : null);
        if (symbol is INamedTypeSymbol type) {
            if (!IsQualifier(name) && !IsCreated(name)) {
                Add(type, KindOf(name, type), name, null);
            }
        } else if (name.Parent is AttributeSyntax || name.Parent is QualifiedNameSyntax { Parent: AttributeSyntax }) {
            Add(symbol?.ContainingType, ReferenceKind.Attribute, name, null);
        } else {
            Used(symbol, name);
        }
    }

    void Used(ISymbol? symbol, SyntaxNode node) {
        switch (symbol) {
            case IMethodSymbol method when method.MethodKind is not MethodKind.BuiltinOperator and not MethodKind.LocalFunction:
                var owning = (method.ReducedFrom ?? method).ContainingType;
                Add(owning, ReferenceKind.Call, node, Signature(method));
                AddArguments(method.TypeArguments.Concat(ArgumentsOf(owning)), node);
                break;
            case IPropertySymbol or IFieldSymbol or IEventSymbol:
                Add(symbol.ContainingType, IsInNameOf(node) ? ReferenceKind.NameOf : ReferenceKind.Member, node, symbol.Name);
                AddArguments(ArgumentsOf(symbol.ContainingType), node);
                break;
        }
    }

    void Inferred(ISymbol? symbol, SyntaxNode node) {
        var type = symbol switch {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => null,
        };
        AddAll(type, ReferenceKind.Inferred, node);
    }

    void AddArguments(IEnumerable<ITypeSymbol> arguments, SyntaxNode node) {
        foreach (var argument in arguments) {
            AddAll(argument, ReferenceKind.TypeArgument, node);
        }
    }

    void AddAll(ITypeSymbol? type, ReferenceKind kind, SyntaxNode node) {
        foreach (var named in Symbols.Named(type)) {
            Add(named, kind, node, null);
        }
    }

    void Add(INamedTypeSymbol? type, ReferenceKind kind, SyntaxNode node, string? targetMember) {
        if (type is null or { TypeKind: TypeKind.Error } or { IsAnonymousType: true } or { IsTupleType: true }) {
            return;
        }
        var target = Symbols.NameOf(type);
        var key = new Key(target, kind, member, targetMember);
        if (target == owner || found.ContainsKey(key)) {
            return;
        }
        var position = places.PositionOf(node.GetLocation());
        found.Add(key, new AppReference(file, new ReferenceFact(target, kind, position.Line, position.Column, member, targetMember)));
    }

    static IEnumerable<ITypeSymbol> ArgumentsOf(INamedTypeSymbol? type) => type is null ? [] : Symbols.Arguments(type);

    static string Signature(IMethodSymbol method) {
        var name = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name;
        return $"{name}({string.Join(",", method.OriginalDefinition.Parameters.Select(p => p.Type.ToDisplayString()))})";
    }

    static bool IsInNameOf(SyntaxNode node) => node.Ancestors().OfType<InvocationExpressionSyntax>()
        .Any(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });

    static bool IsCreated(SimpleNameSyntax name) =>
        Outer(name).Parent is ObjectCreationExpressionSyntax creation && creation.Type == Outer(name);

    static SyntaxNode Outer(SimpleNameSyntax name) =>
        name.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax ? name.Parent : name;

    bool IsQualifier(SimpleNameSyntax name) {
        var right = name.Parent switch {
            QualifiedNameSyntax qualified when qualified.Left == name => qualified.Right,
            QualifiedNameSyntax { Parent: QualifiedNameSyntax outer } qualified when outer.Left == qualified => outer.Right,
            MemberAccessExpressionSyntax access when access.Expression == name => access.Name,
            MemberAccessExpressionSyntax { Parent: MemberAccessExpressionSyntax outer } access when outer.Expression == access => outer.Name,
            _ => null,
        };
        return right is not null && model.GetSymbolInfo(right).Symbol is ITypeSymbol;
    }

    ReferenceKind KindOf(SimpleNameSyntax name, INamedTypeSymbol type) {
        foreach (var node in name.Ancestors()) {
            switch (node) {
                case TypeArgumentListSyntax:
                    return ReferenceKind.TypeArgument;
                case TypeOfExpressionSyntax:
                    return ReferenceKind.TypeOf;
                case BaseListSyntax:
                    return type.TypeKind == TypeKind.Interface ? ReferenceKind.Interface : ReferenceKind.Base;
                case TypeParameterConstraintClauseSyntax:
                    return ReferenceKind.Constraint;
                case AttributeSyntax:
                    return ReferenceKind.Attribute;
                case InvocationExpressionSyntax when IsInNameOf(name):
                    return ReferenceKind.NameOf;
                case StatementSyntax or ExpressionSyntax and not TypeSyntax or EqualsValueClauseSyntax:
                    return ReferenceKind.Type;
                case ParameterSyntax or MemberDeclarationSyntax:
                    return ReferenceKind.Signature;
            }
        }
        return ReferenceKind.Type;
    }

    sealed record Key(TypeName Target, ReferenceKind Kind, string? Member, string? TargetMember);
}
