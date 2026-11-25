namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

sealed class ProjectRun {
    const string ModeProperty = "build_property.UmpireCheckMode";

    readonly StateFolder state;
    readonly ProjectFacts facts;
    readonly AppModel app;
    readonly bool saves;
    readonly IReadOnlyList<string> notCompiled;

    ProjectRun(StateFolder state, ProjectFacts facts, AppModel app, bool saves) {
        this.state = state;
        this.facts = facts;
        this.app = app;
        this.saves = saves;
        notCompiled = saves ? facts.NotCompiled() : [];
    }

    public static ProjectRun? Start(Compilation compilation, AnalyzerOptions options, CancellationToken cancellation) {
        if (BlockSearch.Find(compilation, options, cancellation) is not { } state) {
            return null;
        }
        var settings = state.ReadBlock();
        if (!settings.Projects.Contains(compilation.AssemblyName ?? "", StringComparer.OrdinalIgnoreCase)) {
            return null;
        }
        var config = options.AnalyzerConfigOptionsProvider.GlobalOptions;
        config.TryGetValue(BlockSearch.FolderProperty, out var folder);
        config.TryGetValue(ModeProperty, out var given);
        var facts = new ProjectFacts(compilation, state.BlockOf(settings), string.IsNullOrEmpty(folder) ? null : folder);
        var app = AppModel.Load(state, settings, facts.Current, Enum.TryParse<CheckMode>(given, ignoreCase: true, out var mode) ? mode : null);
        return new ProjectRun(state, facts, app, BuildHost.Saves(config));
    }

    public void Check(SymbolAnalysisContext context) {
        if (app.Find(Symbols.NameOf((INamedTypeSymbol)context.Symbol)) is not { } type || type.Project != facts.Name) {
            return;
        }
        var report = new Report();
        LocationCheck.Run(app, [type], report);
        ContentCheck.Run(app, [type], report);
        ReferenceCheck.Run(app, [type], report);
        foreach (var finding in report.Findings) {
            context.ReportDiagnostic(Diagnostic(finding));
        }
    }

    public void End(CompilationAnalysisContext context) {
        var referenced = context.Compilation.ReferencedAssemblyNames.Select(a => a.Name).ToList();
        foreach (var problem in app.Problems.Where(p => p.IsState || p.IsLibrary || referenced.Contains(p.Project))) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.NoSnapshot, DataCheck.Text(problem)));
        }
        foreach (var library in app.Trust.OldLibraries) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.OldSnapshot, DataCheck.Text(library)));
        }
        foreach (var project in app.Saved.Select(s => s.Snapshot.Project).Where(p => referenced.Contains(p) && !app.Trust.IsSure(p))) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.OldSnapshot, DataCheck.OldText(project)));
        }
        if (DataCheck.BlockText(app) is { } text) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.OldSnapshot, text));
        }
        if (!saves || context.Compilation.GetDiagnostics(context.CancellationToken).Any(d => d.DefaultSeverity == DiagnosticSeverity.Error)) {
            return;
        }
        try {
            state.Save(app.WithSurfaces(facts.Full(notCompiled)));
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.NotSaved, $"The snapshot of {facts.Name} is not saved: {e.Message}"));
        }
    }

    public static Diagnostic DataDiagnostic(string code, string text) =>
        Microsoft.CodeAnalysis.Diagnostic.Create(CheckDescriptors.Of(code), Location.None, text);

    Diagnostic Diagnostic(Finding finding) => Microsoft.CodeAnalysis.Diagnostic.Create(
        CheckDescriptors.Of(finding.Code), facts.LocationOf(finding), SeverityOf(finding) switch {
            Severity.Error => DiagnosticSeverity.Error,
            Severity.Warning => DiagnosticSeverity.Warning,
            _ => DiagnosticSeverity.Info,
        }, additionalLocations: null, properties: null, finding.Text);

    Severity SeverityOf(Finding finding) =>
        finding is { Code: RuleCodes.Count, Severity: Severity.Error } && (app.Settings.Projects.Count > 1 || app.Settings.OtherStates.Count > 0)
            ? Severity.Warning
            : finding.Severity;
}
