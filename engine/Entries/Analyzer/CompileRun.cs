namespace UmpireEngine;

using System.Collections.Concurrent;
using CodeCheck.Slices.CheckCode;
using Facts.Domain.Shared.Compile;
using Jobs.Slices.CompileJob;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Operations.Domain.Shared;
using Operations.Infrastructure;

sealed class CompileRun {
    const string FolderProperty = "build_property.ProjectDir";
    const string StateProperty = "build_property.UmpireCheckState";
    const string ModeProperty = "build_property.UmpireCheckMode";
    const string FrameworkProperty = "build_property.UmpireFrameworkCode";
    const string RequestType = "Umpire.Vsa.Contracts.Requests.IRequest`1";

    readonly Compilation compilation;
    readonly CompileProject project;
    readonly INamedTypeSymbol? request;
    readonly ConcurrentQueue<Diagnostic> atEnd = new();
    readonly Dictionary<string, SyntaxTree> trees = new(StringComparer.Ordinal);
    readonly HashSet<SyntaxTree> generated = [];
    int failed;
    bool inModel;
    bool checksTypes;

    CompileRun(Compilation compilation, CompileProject project, AnalyzerOptions options, CancellationToken cancellation) {
        this.compilation = compilation;
        this.project = project;
        request = compilation.GetTypeByMetadataName(RequestType);
        foreach (var tree in compilation.SyntaxTrees.Where(tree => !trees.ContainsKey(tree.FilePath))) {
            trees.Add(tree.FilePath, tree);
        }
        generated.UnionWith(compilation.SyntaxTrees.Where(tree => GeneratedFiles.Is(tree, options, cancellation)));
    }

    public static CompileRun Start(Compilation compilation, AnalyzerOptions options, CancellationToken cancellation) {
        var global = options.AnalyzerConfigOptionsProvider.GlobalOptions;
        var config = ConfigFileOf(options);
        var project = new CompileProject(
            compilation.AssemblyName ?? "",
            config?.Path ?? "",
            config?.GetText(cancellation)?.ToString() ?? "",
            string.Equals(Value(global, FrameworkProperty), "true", StringComparison.OrdinalIgnoreCase),
            [.. CodeRule.All.Select(rule => rule.Id).Where(id => RuleSuppression.IsOff(id, compilation, cancellation))],
            [.. compilation.ReferencedAssemblyNames.Select(assembly => assembly.Name)],
            new ModelSettings(
                Value(global, FolderProperty), Value(global, StateProperty), Value(global, ModeProperty),
                SourceFolders(compilation), BuildHost.Saves(global)));
        CompileProjects.Add(project, compilation);
        var run = new CompileRun(compilation, project, options, cancellation);
        var report = CompileApp.Send(new CompileJobRequest(project, CompileStage.Start), cancellation);
        run.inModel = report.InModel;
        run.checksTypes = report.ChecksTypes;
        foreach (var diagnostic in run.Diagnostics(report)) {
            run.atEnd.Enqueue(diagnostic);
        }
        return run;
    }

    public void File(SemanticModelAnalysisContext context) {
        if (context.IsGeneratedCode) {
            return;
        }
        var file = CodeFileReader.Read(context.SemanticModel, context.CancellationToken);
        Report(new CompileJobRequest(project, CompileStage.File, File: file), context.ReportDiagnostic, context.CancellationToken);
    }

    public void Type(SymbolAnalysisContext context) {
        if (!checksTypes) {
            return;
        }
        var type = CodeTypeReader.Read(
            (INamedTypeSymbol)context.Symbol, context.IsGeneratedCode ? null : request, compilation, generated.Contains);
        Report(new CompileJobRequest(project, CompileStage.Type, Type: type), context.ReportDiagnostic, context.CancellationToken);
    }

    public void End(CompilationAnalysisContext context) {
        var hasErrors = inModel && project.Model.Saves
            && context.Compilation.GetDiagnostics(context.CancellationToken).Any(d => d.DefaultSeverity == DiagnosticSeverity.Error);
        Report(new CompileJobRequest(project, CompileStage.End, HasErrors: hasErrors), context.ReportDiagnostic, context.CancellationToken);
        while (atEnd.TryDequeue(out var diagnostic)) {
            context.ReportDiagnostic(diagnostic);
        }
    }

    static AdditionalText? ConfigFileOf(AnalyzerOptions options) =>
        options.AdditionalFiles.FirstOrDefault(file =>
            string.Equals(Path.GetFileName(file.Path), Umpire.Config.UmpireConfig.FileName, StringComparison.OrdinalIgnoreCase));

    static string? Value(AnalyzerConfigOptions options, string key) =>
        options.TryGetValue(key, out var value) && value.Length > 0 ? value : null;

    static IReadOnlyList<string> SourceFolders(Compilation compilation) => [.. compilation.SyntaxTrees
        .Select(tree => tree.FilePath).Where(Path.IsPathRooted).Select(Path.GetDirectoryName)
        .OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];

    void Report(CompileJobRequest job, Action<Diagnostic> report, CancellationToken cancellation) {
        CompileJobReport result;
        try {
            result = CompileApp.Send(job, cancellation);
        } catch (Exception e) when (CompileApp.IsFailure(e)) {
            if (Interlocked.Exchange(ref failed, 1) == 0) {
                atEnd.Enqueue(CompileApp.IsLoadFailure(e)
                    ? Descriptors.LoadFailure(CompileApp.Reason(e))
                    : Descriptors.Failure($"The compile job of {project.Name} failed: {CompileApp.Reason(e)}"));
            }
            return;
        }
        foreach (var diagnostic in Diagnostics(result)) {
            report(diagnostic);
        }
    }

    IEnumerable<Diagnostic> Diagnostics(CompileJobReport report) {
        foreach (var finding in report.Findings) {
            var location = LocationOf(finding);
            if (Descriptors.Of(finding.Code) is not { } descriptor) {
                yield return Descriptors.Failure($"{finding.Code}: {finding.Message}", location);
                continue;
            }
            yield return finding.Severity == FindingSeverity.Warning || finding.Code == RunCodes.CouldNotRun
                ? Diagnostic.Create(descriptor, location, finding.Message)
                : Diagnostic.Create(descriptor, location, SeverityOf(finding.Severity), additionalLocations: null, properties: null, finding.Message);
        }
    }

    Location LocationOf(Finding finding) {
        if (finding.File is not { } file) {
            return Location.None;
        }
        if (finding.Length is not { } length || !trees.TryGetValue(file, out var tree)) {
            return ProjectFacts.LocationOf(compilation, file, finding.Line ?? 1, finding.Column ?? 1);
        }
        var text = tree.GetText();
        var start = text.Lines[(finding.Line ?? 1) - 1].Start + (finding.Column ?? 1) - 1;
        return Location.Create(tree, new TextSpan(start, length));
    }

    static DiagnosticSeverity SeverityOf(FindingSeverity severity) => severity switch {
        FindingSeverity.Error => DiagnosticSeverity.Error,
        FindingSeverity.Info => DiagnosticSeverity.Info,
        _ => DiagnosticSeverity.Warning,
    };
}
