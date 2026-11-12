using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;

namespace VSA.Architecture.Engine;

public sealed record Exposure(IType Target, Access Access, string Member);

public static class Exposures {
    static readonly Dictionary<string, GenericArgument> NoArguments = [];

    public static IEnumerable<Exposure> Of(IType type, Func<IType, bool> isFramework) {
        var typeAccess = AccessOf(type.Visibility);
        var frameworkInterfaces = new List<ImplementedInterface>();
        foreach (var dependency in type.Dependencies) {
            if (dependency is not (InheritsBaseClassDependency or ImplementsInterfaceDependency)) {
                continue;
            }
            var arguments = (dependency as ITypeDependency)?.TargetGenericArguments.ToList() ?? [];
            if (isFramework(dependency.Target)) {
                if (dependency is ImplementsInterfaceDependency) {
                    var byParameter = ArgumentsByParameter(dependency.Target, arguments);
                    frameworkInterfaces.Add(new ImplementedInterface(dependency.Target, byParameter));
                }
                continue;
            }
            foreach (var target in Flatten(dependency.Target, arguments)) {
                yield return new Exposure(target, typeAccess, "base type");
            }
        }
        var members = type.Members.Where(m => !m.IsCompilerGenerated && !frameworkInterfaces.Any(f => Implements(m, f)));
        foreach (var member in members) {
            var access = typeAccess.Narrowest(AccessOf(member.Visibility));
            foreach (var target in SignatureTypes(member)) {
                yield return new Exposure(target, access, member.Name);
            }
        }
    }

    static IReadOnlyDictionary<string, GenericArgument> ArgumentsByParameter(
        IType type, IReadOnlyList<GenericArgument> arguments) =>
        type.GenericParameters.Zip(arguments).ToDictionary(pair => pair.First.Name, pair => pair.Second);

    static bool Implements(IMember member, ImplementedInterface implemented) =>
        Signature(member, NoArguments) is { } signature
        && implemented.Type.Members.Any(declared =>
            ShortName(declared) == ShortName(member) && Signature(declared, implemented.Arguments) == signature);

    static string ShortName(IMember member) {
        var name = member.Name.Split('(')[0];
        return name[(name.LastIndexOf('.') + 1)..];
    }

    static string? Signature(IMember member, IReadOnlyDictionary<string, GenericArgument> arguments) => member switch {
        MethodMember method => string.Join(",", new[] { method.ReturnTypeInstance }.Concat(method.ParameterInstances)
            .Select(instance => Shape(instance.Type, instance.GenericArguments, arguments))),
        PropertyMember property => Shape(property.Type, property.GenericArguments, arguments),
        _ => null,
    };

    static string Shape(
        IType type, IEnumerable<GenericArgument> generics, IReadOnlyDictionary<string, GenericArgument> arguments) {
        if (type is GenericParameter && arguments.TryGetValue(type.Name, out var argument)) {
            return Shape(argument.Type, argument.GenericArguments, NoArguments);
        }
        var shapes = generics.Select(generic => Shape(generic.Type, generic.GenericArguments, arguments)).ToList();
        return shapes.Count == 0 ? type.FullName : $"{type.FullName}<{string.Join(",", shapes)}>";
    }

    static IEnumerable<IType> SignatureTypes(IMember member) => member switch {
        MethodMember { MethodForm: MethodForm.Getter or MethodForm.Setter } => [],
        MethodMember method => new[] { method.ReturnTypeInstance }.Concat(method.ParameterInstances)
            .SelectMany(instance => Flatten(instance.Type, instance.GenericArguments)),
        PropertyMember property => Flatten(property.Type, property.GenericArguments),
        FieldMember field => Flatten(field.Type, field.GenericArguments),
        _ => [],
    };

    static IEnumerable<IType> Flatten(IType type, IEnumerable<GenericArgument> arguments) =>
        new[] { type }.Concat(arguments.SelectMany(argument => Flatten(argument.Type, argument.GenericArguments)));

    public static Access AccessOf(Visibility visibility) => visibility switch {
        Visibility.Public => Access.Public,
        Visibility.ProtectedInternal => Access.ProtectedInternal,
        Visibility.Internal => Access.Internal,
        Visibility.Protected => Access.Protected,
        Visibility.PrivateProtected => Access.PrivateProtected,
        _ => Access.Private,
    };

    sealed record ImplementedInterface(IType Type, IReadOnlyDictionary<string, GenericArgument> Arguments);
}
