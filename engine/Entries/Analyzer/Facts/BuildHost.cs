namespace UmpireEngine;

using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

static class BuildHost {
    public const string SaveProperty = "build_property.UmpireSaveSnapshot";
    const string DesignTimeProperty = "build_property.DesignTimeBuild";
    const string On = "true";

    static readonly string[] Compilers = ["csc", "VBCSCompiler"];

    public static bool Saves(AnalyzerConfigOptions options) {
        if (options.TryGetValue(DesignTimeProperty, out var design) && Is(design)) {
            return false;
        }
        return options.TryGetValue(SaveProperty, out var save) && save.Length > 0
            ? Is(save)
            : IsCompiler(Assembly.GetEntryAssembly()?.GetName().Name);
    }

    public static bool IsCompiler(string? process) => Compilers.Contains(process, StringComparer.OrdinalIgnoreCase);

    static bool Is(string value) => string.Equals(value, On, StringComparison.OrdinalIgnoreCase);
}
