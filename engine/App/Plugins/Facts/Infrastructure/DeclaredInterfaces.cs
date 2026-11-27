namespace Facts.Infrastructure;

using System.Reflection.Metadata.Ecma335;
using Facts.Domain.Shared;

static class DeclaredInterfaces {
    public static List<IReadOnlyList<TypeName>> Names(Type type, DllContext context) {
        var metadata = context.Symbols.Metadata;
        return [.. metadata.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(type.MetadataToken)).GetInterfaceImplementations()
            .Select(i => context.Names.Of(metadata.GetInterfaceImplementation(i).Interface))
            .Where(names => names.Count > 0)];
    }

    public static bool IsDeclared(Type implemented, IReadOnlyList<IReadOnlyList<TypeName>> declared) {
        var names = ReflectionShapes.Named(implemented).Select(t => ReflectionShapes.NameOf(t).FullName).ToList();
        return declared.Any(d => d.Select(n => n.FullName).SequenceEqual(names));
    }
}
