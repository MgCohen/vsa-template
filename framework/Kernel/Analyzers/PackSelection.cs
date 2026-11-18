namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static class PackSelection {
    public const string ArchitectureKey = "umpire.architecture";
    const string MetadataKey = "Umpire.Architecture";
    const string MetadataAttribute = "System.Reflection.AssemblyMetadataAttribute";
    const string AppType = "Umpire.Kernels.App`1";
    const string BlockType = "Umpire.Architecture.Block`1";
    const string PackType = "Umpire.Kernels.Pack`2";

    public static bool Selects(Compilation compilation, AnalyzerConfigOptions global, string packAssembly) {
        var entries = CodeEntries(compilation).ToList();
        if (entries.Count == 0 && global.TryGetValue(ArchitectureKey, out var named) && named.Trim().Length > 0) {
            entries.Add(NamedEntry(compilation, named));
        }
        return entries.Count == 0
            ? compilation.ReferencedAssemblyNames.Any(reference => reference.Name == packAssembly)
            : entries.Any(assemblies => assemblies.Contains(packAssembly));
    }

    static IEnumerable<IReadOnlyList<string>> CodeEntries(Compilation compilation) {
        var bases = new[] { AppType, PackType, BlockType }.Select(compilation.GetTypeByMetadataName).ToArray();
        foreach (var type in SourceTypes(compilation.Assembly.GlobalNamespace)) {
            if (EntryBase(type, bases) is { } entry && entry.TypeArguments[0].TypeKind != TypeKind.TypeParameter) {
                yield return Assemblies(entry.TypeArguments[0]);
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

    static IReadOnlyList<string> NamedEntry(Compilation compilation, string named) {
        var parts = named.Split(',');
        return compilation.GetTypeByMetadataName(parts[0].Trim()) is { } type ? Assemblies(type)
            : parts.Length > 1 ? [parts[1].Trim()]
            : [];
    }

    static INamedTypeSymbol? EntryBase(INamedTypeSymbol type, INamedTypeSymbol?[] bases) {
        for (var current = type.BaseType; current is not null; current = current.BaseType) {
            if (bases.Any(entry => SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, entry))) {
                return current;
            }
        }
        return null;
    }

    static IReadOnlyList<string> Assemblies(ITypeSymbol type) {
        var names = new List<string>();
        for (var current = type; current is not null; current = current.BaseType) {
            if (current.ContainingAssembly is { } assembly) {
                names.Add(assembly.Name);
            }
        }
        return names;
    }

    static IEnumerable<INamedTypeSymbol> SourceTypes(INamespaceSymbol space) =>
        space.GetTypeMembers().SelectMany(WithNested).Where(type => type.TypeKind == TypeKind.Class)
            .Concat(space.GetNamespaceMembers().SelectMany(SourceTypes));

    static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type) =>
        new[] { type }.Concat(type.GetTypeMembers().SelectMany(WithNested));
}
