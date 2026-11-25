namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;

static class MemberReader {
    const string CompilerName = "<";

    public static IEnumerable<ISymbol> MembersOf(INamedTypeSymbol type) => type.GetMembers().Where(IsMember);

    public static MemberFact Fact(ISymbol member, IReadOnlyList<TypeName> implements, int line) {
        var method = member as IMethodSymbol;
        var property = member as IPropertySymbol;
        var field = member as IFieldSymbol;
        return new MemberFact(
            member.Name, KindOf(member), Symbols.AccessOf(member),
            [.. (method?.Parameters ?? property?.Parameters ?? []).Select(Parameter)],
            [.. (method?.TypeParameters ?? []).Select(TypeParameter)],
            implements,
            member.IsStatic, member.IsAbstract, member.IsVirtual, member.IsOverride, member.IsSealed,
            field?.IsReadOnly ?? property?.IsReadOnly ?? method?.IsReadOnly ?? false,
            field?.IsConst ?? false, method?.IsAsync ?? false, member.IsImplicitlyDeclared, line,
            TypeOf(member) is { } type ? Shapes.Of(type) : null,
            property?.GetMethod is { } get ? Symbols.AccessOf(get) : null,
            property?.SetMethod is { } set ? Symbols.AccessOf(set) : null);
    }

    public static TypeParameterFact TypeParameter(ITypeParameterSymbol parameter) {
        var limits = new[] {
            parameter.HasReferenceTypeConstraint ? "class" : "",
            parameter.HasValueTypeConstraint ? "struct" : "",
            parameter.HasUnmanagedTypeConstraint ? "unmanaged" : "",
            parameter.HasNotNullConstraint ? "notnull" : "",
            parameter.HasConstructorConstraint ? "new()" : "",
            parameter.Variance switch {
                VarianceKind.In => "in",
                VarianceKind.Out => "out",
                _ => "",
            },
        };
        return new TypeParameterFact(parameter.Name, [.. limits.Where(l => l.Length > 0)], [.. parameter.ConstraintTypes.Select(Shapes.Of)]);
    }

    static bool IsMember(ISymbol member) => member switch {
        IFieldSymbol field => field.AssociatedSymbol is null && !field.Name.StartsWith(CompilerName, StringComparison.Ordinal),
        IMethodSymbol method => method.AssociatedSymbol is null
            && !(method is { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor, IsImplicitlyDeclared: true, Parameters.Length: 0 }),
        IPropertySymbol or IEventSymbol => true,
        _ => false,
    };

    static MemberKind KindOf(ISymbol member) => member switch {
        IFieldSymbol => MemberKind.Field,
        IPropertySymbol => MemberKind.Property,
        IEventSymbol => MemberKind.Event,
        IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } => MemberKind.Constructor,
        IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion } => MemberKind.Operator,
        IMethodSymbol { MethodKind: MethodKind.Destructor } => MemberKind.Destructor,
        _ => MemberKind.Method,
    };

    static ITypeSymbol? TypeOf(ISymbol member) => member switch {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        IEventSymbol e => e.Type,
        IMethodSymbol { MethodKind: not (MethodKind.Constructor or MethodKind.StaticConstructor) } method => method.ReturnType,
        _ => null,
    };

    static ParameterFact Parameter(IParameterSymbol parameter) => new(
        parameter.Name, Shapes.Of(parameter.Type), parameter.IsOptional, parameter.IsParams,
        parameter.RefKind == RefKind.None ? null : parameter.RefKind.ToString().ToLowerInvariant());
}
