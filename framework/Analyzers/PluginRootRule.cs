using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1014: the root class of a plugin module (VsaRootFolder = Plugins) inherits Plugin, and the root class of a module
// (VsaRootFolder = Features) does not. The host configures the plugin modules first, and the name of the base says which one it is.
public sealed class PluginRootRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1014",
        "A plugin root inherits Plugin, and a module root inherits Module",
        "{0} is the root class of the {1} {2}, but it inherits {3}. Make it inherit {4}.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) {
        var options = start.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        if (!options.TryGetValue("build_property.VsaRootFolder", out var root) || root is not ("Plugins" or "Features")) {
            return;
        }
        var module = start.Compilation.GetTypeByMetadataName("VSA.Module.Module");
        var plugin = start.Compilation.GetTypeByMetadataName("VSA.Module.Plugin");
        if (module is null || plugin is null) {
            return;
        }
        var isPlugin = root == "Plugins";
        start.RegisterSymbolAction(context => {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class || type.IsAbstract || !Inherits(type, module)
                || Inherits(type, plugin) == isPlugin) {
                return;
            }
            var name = start.Compilation.AssemblyName ?? "";
            context.ReportDiagnostic(isPlugin
                ? Create(type.Locations[0], type.Name, "plugin module", name, "Module", "Plugin or Plugin<TSettings>")
                : Create(type.Locations[0], type.Name, "module", name, "Plugin", "Module or Module<TSettings>, or move the module to Plugins/"));
        }, SymbolKind.NamedType);
    }

    static bool Inherits(INamedTypeSymbol type, INamedTypeSymbol root) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (SymbolEqualityComparer.Default.Equals(b.OriginalDefinition, root)) {
                return true;
            }
        }
        return false;
    }
}
