using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1003: a contracts project holds only contracts. In <Module>.Contracts.Requests, <Module>.Contracts.Commands and
// <Module>.Contracts.Events, each public type is a request, a response, a command, an event, or a type that one of
// them uses (a DTO). A command result has no marker. It passes because its command uses it.
// In a slice module, VSA.Sdk selects the project of each file by its types (VsaSliceRouter.cs).
// This rule checks the result, so a helper type that is in the same file as a contract cannot become public API.
// A Shared module (VsaRootFolder = Shared) is not checked: its public types are bases for the modules.
public sealed class ContractTypeRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1003",
        "Contracts projects hold only contract types",
        "{0} is in {1}, but no request, response, command or event uses it. Move it to its own file in the slice, or use it in a contract.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) {
        var assembly = start.Compilation.AssemblyName ?? "";
        if (!assembly.EndsWith(".Contracts.Requests", System.StringComparison.Ordinal)
            && !assembly.EndsWith(".Contracts.Commands", System.StringComparison.Ordinal)
            && !assembly.EndsWith(".Contracts.Events", System.StringComparison.Ordinal)) {
            return;
        }
        if (start.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.VsaRootFolder", out var root)
            && root == "Shared") {
            return;
        }
        var markers = new[] {
            start.Compilation.GetTypeByMetadataName("VSA.Contracts.Requests.IRequest`1"),
            start.Compilation.GetTypeByMetadataName("VSA.Contracts.Requests.IResponse"),
            start.Compilation.GetTypeByMetadataName("VSA.Contracts.Commands.ICommand`1"),
            start.Compilation.GetTypeByMetadataName("VSA.Contracts.Events.IntegrationEvent"),
        }.OfType<INamedTypeSymbol>().ToArray();
        if (markers.Length == 0) {
            return;
        }
        var used = Contracts(start.Compilation.Assembly, markers);
        start.RegisterSymbolAction(context => {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.DeclaredAccessibility == Accessibility.Public && !type.IsImplicitlyDeclared
                && !used.Contains(type) && type.Locations.FirstOrDefault(l => l.IsInSource) is { } location) {
                context.ReportDiagnostic(Create(location, type.Name, assembly));
            }
        }, SymbolKind.NamedType);
    }

    // The marked types of the assembly and each type of the assembly that they use, to any depth.
    static HashSet<INamedTypeSymbol> Contracts(IAssemblySymbol assembly, INamedTypeSymbol[] markers) {
        var all = Types(assembly.GlobalNamespace).ToList();
        var found = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var queue = new Queue<INamedTypeSymbol>(all.Where(t => IsMarked(t, markers)));
        while (queue.Count > 0) {
            var type = queue.Dequeue();
            if (!found.Add(type)) {
                continue;
            }
            foreach (var next in Uses(type).SelectMany(Named).Where(t => SymbolEqualityComparer.Default.Equals(t.ContainingAssembly, assembly))) {
                queue.Enqueue(next.OriginalDefinition);
            }
        }
        return found;
    }

    static bool IsMarked(INamedTypeSymbol type, INamedTypeSymbol[] markers) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (markers.Any(m => SymbolEqualityComparer.Default.Equals(b.OriginalDefinition, m))) {
                return true;
            }
        }
        return type.AllInterfaces.Any(i => markers.Any(m => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, m)));
    }

    // The types in the signature of a type: its bases, and its public fields, properties and method parameters.
    static IEnumerable<ITypeSymbol> Uses(INamedTypeSymbol type) {
        if (type.BaseType is { } baseType) {
            yield return baseType;
        }
        foreach (var i in type.Interfaces) {
            yield return i;
        }
        foreach (var member in type.GetMembers().Where(m => m.DeclaredAccessibility == Accessibility.Public)) {
            switch (member) {
                case IFieldSymbol field:
                    yield return field.Type;
                    break;
                case IPropertySymbol property:
                    yield return property.Type;
                    break;
                case IMethodSymbol method:
                    yield return method.ReturnType;
                    foreach (var p in method.Parameters) {
                        yield return p.Type;
                    }
                    break;
                case INamedTypeSymbol nested:
                    yield return nested;
                    break;
            }
        }
    }

    // A type and its element and type arguments: IReadOnlyList<NoteDto> gives IReadOnlyList and NoteDto.
    static IEnumerable<INamedTypeSymbol> Named(ITypeSymbol type) {
        switch (type) {
            case IArrayTypeSymbol array:
                foreach (var t in Named(array.ElementType)) {
                    yield return t;
                }
                break;
            case INamedTypeSymbol named:
                yield return named;
                foreach (var t in named.TypeArguments.SelectMany(Named)) {
                    yield return t;
                }
                break;
        }
    }

    static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns) {
        foreach (var member in ns.GetMembers()) {
            if (member is INamespaceSymbol child) {
                foreach (var t in Types(child)) {
                    yield return t;
                }
            } else if (member is INamedTypeSymbol type) {
                yield return type;
            }
        }
    }
}
