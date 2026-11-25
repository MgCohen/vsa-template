namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Umpire.Vsa.Analyzers;

static class BlockSearch {
    public const string FolderProperty = "build_property.ProjectDir";
    const string StateProperty = "build_property.UmpireCheckState";

    public static StateFolder? Find(Compilation compilation, AnalyzerOptions options, CancellationToken cancellation) {
        var config = options.AnalyzerConfigOptionsProvider.GlobalOptions;
        if (config.TryGetValue(StateProperty, out var given) && given.Length > 0) {
            config.TryGetValue(FolderProperty, out var project);
            return StateFolder.At(Path.Combine(project ?? "", given)) is { HasDefinition: true } state ? state : null;
        }
        if (ConfigFile.File(options) is { Path: { Length: > 0 } file }
            && Path.GetDirectoryName(Path.GetFullPath(file)) is { } folder
            && StateFolder.Of(folder, Configured(options, cancellation)) is { HasDefinition: true } configured) {
            return configured;
        }
        return compilation.SyntaxTrees.Select(t => t.FilePath).Where(f => Path.IsPathRooted(f))
            .Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(StateFolder.Above).FirstOrDefault(state => state is not null);
    }

    static string? Configured(AnalyzerOptions options, CancellationToken cancellation) {
        try {
            return ConfigFile.Of(options, cancellation).Value(StateFolder.Key);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return null;
        }
    }
}
