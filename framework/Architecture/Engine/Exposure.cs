using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;

namespace VSA.Architecture.Engine;

public sealed record Exposure(IType Target, Access Access, string Member);

public static class Exposures {
    public static IEnumerable<Exposure> Of(IType type) {
        var typeAccess = AccessOf(type.Visibility);
        foreach (var dependency in type.Dependencies) {
            if (dependency is InheritsBaseClassDependency or ImplementsInterfaceDependency) {
                foreach (var target in Flatten(dependency.Target, (dependency as ITypeDependency)?.TargetGenericArguments ?? [])) {
                    yield return new Exposure(target, typeAccess, "base type");
                }
            }
        }
        foreach (var member in type.Members.Where(m => !m.IsCompilerGenerated)) {
            var access = typeAccess.Narrowest(AccessOf(member.Visibility));
            foreach (var target in SignatureTypes(member)) {
                yield return new Exposure(target, access, member.Name);
            }
        }
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
}
