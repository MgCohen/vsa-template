namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis.Diagnostics;

static class BuildHost {
    public const string SaveProperty = "build_property.UmpireSaveSnapshot";
    const string DesignTimeProperty = "build_property.DesignTimeBuild";
    const string EditorAssembly = "Microsoft.CodeAnalysis.Workspaces";
    const string On = "true";

    public static bool Saves(AnalyzerConfigOptions options) {
        if (options.TryGetValue(SaveProperty, out var save) && save.Length > 0) {
            return Is(save);
        }
        return !(options.TryGetValue(DesignTimeProperty, out var design) && Is(design))
            && !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == EditorAssembly);
    }

    static bool Is(string value) => string.Equals(value, On, StringComparison.OrdinalIgnoreCase);
}
