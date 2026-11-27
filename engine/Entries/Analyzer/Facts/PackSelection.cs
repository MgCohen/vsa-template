namespace UmpireEngine;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public static class PackSelection {
    public const string ArchitectureKey = "umpire.architecture";
    const string MetadataKey = "Umpire.Architecture";
    const string MetadataAttribute = "System.Reflection.AssemblyMetadataAttribute";
    const string AppType = "Umpire.Kernels.App`1";
    const string BlockType = "Umpire.Architecture.Block`1";
    const string ArchitectureType = "Umpire.Kernels.Architecture`1";
    const string KernelType = "Umpire.Kernels.Kernel";
    const string AppEntryType = "Umpire.Kernels.IAppEntry`1";

    public static bool Selects(Compilation compilation, string? architecture, string packNamespace) {
        var entries = CodeEntries(compilation).ToList();
        if (entries.Count == 0 && architecture is { } named && named.Trim().Length > 0) {
            entries.Add(NamedEntry(compilation, named));
        }
        return entries.Count == 0
            ? References(compilation, packNamespace)
            : entries.Any(names => names.Any(name => IsIn(name, packNamespace)));
    }

    static IEnumerable<IReadOnlyList<string>> CodeEntries(Compilation compilation) {
        var bases = new[] { AppType, ArchitectureType, BlockType }.Select(compilation.GetTypeByMetadataName).ToArray();
        var entryInterface = compilation.GetTypeByMetadataName(AppEntryType);
        var kernel = compilation.GetTypeByMetadataName(KernelType);
        foreach (var type in SourceTypes(compilation.Assembly.GlobalNamespace)) {
            IEnumerable<INamedTypeSymbol> entries = EntryBase(type, bases) is { } entryBase
                ? [entryBase]
                : EntryInterfaces(type, entryInterface);
            foreach (var entry in entries.Where(e => e.TypeArguments[0].TypeKind != TypeKind.TypeParameter)) {
                yield return Assemblies(entry.TypeArguments[0]);
            }
            if (EntryBase(type, [kernel]) is not null) {
                yield return Assemblies(type);
            }
        }
        foreach (var attribute in compilation.Assembly.GetAttributes()) {
            var arguments = attribute.ConstructorArguments;
            if (attribute.AttributeClass?.ToDisplayString() == MetadataAttribute && arguments.Length == 2
                && arguments[0].Value is MetadataKey && arguments[1].Value is string named) {
                yield return NamedEntry(compilation, named);
            }
        }
    }

    static List<string> NamedEntry(Compilation compilation, string named) {
        var parts = named.Split(',');
        return compilation.GetTypeByMetadataName(parts[0].Trim()) is { } type ? Assemblies(type)
            : parts.Length > 1 ? [parts[1].Trim()]
            : [];
    }

    static IEnumerable<INamedTypeSymbol> EntryInterfaces(INamedTypeSymbol type, INamedTypeSymbol? entryInterface) =>
        entryInterface is null || type.IsAbstract
            ? []
            : type.AllInterfaces.Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, entryInterface));

    static INamedTypeSymbol? EntryBase(INamedTypeSymbol type, INamedTypeSymbol?[] bases) {
        for (var current = type.BaseType; current is not null; current = current.BaseType) {
            if (bases.Any(entry => SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, entry))) {
                return current;
            }
        }
        return null;
    }

    static List<string> Assemblies(ITypeSymbol type) {
        var names = new List<string>();
        for (var current = type; current is not null; current = current.BaseType) {
            if (current.ContainingAssembly is { } assembly) {
                names.Add(assembly.Name);
            }
            if (current.ContainingNamespace is { IsGlobalNamespace: false } space) {
                names.Add(space.ToDisplayString());
            }
        }
        return names;
    }

    static bool IsIn(string name, string packNamespace) =>
        name == packNamespace || name.StartsWith(packNamespace + ".", System.StringComparison.Ordinal);

    static bool References(Compilation compilation, string packNamespace) =>
        compilation.ReferencedAssemblyNames.Any(reference => reference.Name == packNamespace)
        || HasReferencedType(compilation, compilation.GlobalNamespace, packNamespace.Split('.'), 0);

    static bool HasReferencedType(Compilation compilation, INamespaceSymbol space, string[] parts, int index) =>
        index == parts.Length
            ? HasReferencedType(compilation, space)
            : space.GetNamespaceMembers().Any(child => child.Name == parts[index] && HasReferencedType(compilation, child, parts, index + 1));

    static bool HasReferencedType(Compilation compilation, INamespaceSymbol space) =>
        space.GetTypeMembers().Any(type => !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly))
        || space.GetNamespaceMembers().Any(child => HasReferencedType(compilation, child));

    static IEnumerable<INamedTypeSymbol> SourceTypes(INamespaceSymbol space) =>
        space.GetTypeMembers().SelectMany(WithNested).Where(type => type.TypeKind == TypeKind.Class)
            .Concat(space.GetNamespaceMembers().SelectMany(SourceTypes));

    static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type) =>
        new[] { type }.Concat(type.GetTypeMembers().SelectMany(WithNested));
}
