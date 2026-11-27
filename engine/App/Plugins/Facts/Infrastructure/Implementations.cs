namespace Facts.Infrastructure;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Facts.Domain.Shared;

sealed class Implementations(Type type, DllContext context) {
    sealed record Implemented(int Method, TypeName Interface);

    public ILookup<int, TypeName> Read() {
        if (type.IsInterface) {
            return Array.Empty<Implemented>().ToLookup(i => i.Method, i => i.Interface);
        }
        return Explicit().Concat(Implicit()).ToLookup(i => i.Method, i => i.Interface);
    }

    IEnumerable<Implemented> Explicit() {
        var metadata = context.Symbols.Metadata;
        var definition = metadata.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(type.MetadataToken));
        foreach (var handle in definition.GetMethodImplementations()) {
            var implementation = metadata.GetMethodImplementation(handle);
            if (implementation.MethodBody.Kind == HandleKind.MethodDefinition && OwnerOf(metadata, implementation.MethodDeclaration) is { } owner) {
                yield return new Implemented(MetadataTokens.GetToken(implementation.MethodBody), owner);
            }
        }
    }

    TypeName? OwnerOf(MetadataReader metadata, EntityHandle declaration) => declaration.Kind switch {
        HandleKind.MemberReference => context.Names.Head(metadata.GetMemberReference((MemberReferenceHandle)declaration).Parent),
        HandleKind.MethodDefinition => context.Names.Of(metadata.GetMethodDefinition((MethodDefinitionHandle)declaration).GetDeclaringType()),
        _ => null,
    };

    IEnumerable<Implemented> Implicit() {
        var own = ReflectionFlags.Safe(() => type.GetMethods(ReflectionFlags.Declared), [])
            .Where(m => m is { IsStatic: false, IsPublic: true }).ToList();
        var names = DeclaredInterfaces.Names(type, context);
        var interfaces = ReflectionFlags.Safe(() => type.GetInterfaces(), []).Where(i => DeclaredInterfaces.IsDeclared(i, names));
        foreach (var declared in interfaces) {
            var name = ReflectionShapes.NameOf(declared);
            foreach (var member in ReflectionFlags.Safe(() => declared.GetMethods(), []).Where(m => !m.IsStatic)) {
                if (own.FirstOrDefault(m => m.Name == member.Name && Same(m, member)) is { } found) {
                    yield return new Implemented(found.MetadataToken, name);
                }
            }
        }
    }

    static bool Same(MethodInfo own, MethodInfo declared) => ReflectionFlags.Safe(() => Signature(own) == Signature(declared), false);

    static string Signature(MethodInfo method) => string.Join(",",
        new[] { method.ReturnType }.Concat(method.GetParameters().Select(p => p.ParameterType)).Select(t => ReflectionShapes.Of(t).Text));
}
