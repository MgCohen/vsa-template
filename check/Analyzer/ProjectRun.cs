namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

sealed class ProjectRun {
    readonly StateFolder state;
    readonly ProjectFacts facts;
    readonly AppModel app;
    readonly Freshness freshness;
    readonly bool saves;

    ProjectRun(StateFolder state, ProjectFacts facts, AppModel app, bool saves) {
        this.state = state;
        this.facts = facts;
        this.app = app;
        this.saves = saves;
        freshness = app.Freshness;
    }

    public static ProjectRun? Start(Compilation compilation, AnalyzerOptions options, CancellationToken cancellation) {
        if (BlockSearch.Find(compilation, options, cancellation) is not { } state) {
            return null;
        }
        var settings = BlockSettings.Read(File.ReadAllText(state.BlockFile));
        if (!settings.Projects.Contains(compilation.AssemblyName ?? "", StringComparer.OrdinalIgnoreCase)) {
            return null;
        }
        var config = options.AnalyzerConfigOptionsProvider.GlobalOptions;
        config.TryGetValue(BlockSearch.FolderProperty, out var folder);
        var facts = new ProjectFacts(compilation, state.BlockOf(settings), string.IsNullOrEmpty(folder) ? null : folder);
        return new ProjectRun(state, facts, AppModel.Load(state, facts.Current), BuildHost.Saves(config));
    }

    public void Check(SymbolAnalysisContext context) {
        if (app.Find(Symbols.NameOf((INamedTypeSymbol)context.Symbol)) is not { } type || type.Project != facts.Name) {
            return;
        }
        var own = new Report();
        LocationCheck.Run(app, [type], own);
        ContentCheck.Run(app, [type], own);
        var uses = new Report();
        ReferenceCheck.Run(app, [type], uses);
        var isSure = UsesCurrentData(type);
        foreach (var finding in own.Findings) {
            context.ReportDiagnostic(Diagnostic(finding));
        }
        foreach (var finding in uses.Findings) {
            context.ReportDiagnostic(Diagnostic(isSure && IsSure(finding) ? finding : Doubtful(finding)));
        }
    }

    public void End(CompilationAnalysisContext context) {
        var referenced = context.Compilation.ReferencedAssemblyNames.Select(a => a.Name).ToList();
        foreach (var problem in app.Problems.Where(p => referenced.Contains(p.Project) || !app.Settings.Projects.Contains(p.Project))) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.NoSnapshot, DataCheck.Text(problem)));
        }
        if (!saves || context.Compilation.GetDiagnostics(context.CancellationToken).Any(d => d.DefaultSeverity == DiagnosticSeverity.Error)) {
            return;
        }
        try {
            state.Save(facts.Full());
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            context.ReportDiagnostic(DataDiagnostic(DataCodes.NotSaved, $"The snapshot of {facts.Name} is not saved: {e.Message}"));
        }
    }

    public static Diagnostic DataDiagnostic(string code, string text) =>
        Microsoft.CodeAnalysis.Diagnostic.Create(CheckDescriptors.Of(code), Location.None, text);

    static Finding Doubtful(Finding finding) =>
        finding.Severity == Severity.Error ? finding with { Severity = Severity.Warning } : finding;

    Diagnostic Diagnostic(Finding finding) => Microsoft.CodeAnalysis.Diagnostic.Create(
        CheckDescriptors.Of(finding.Code), facts.LocationOf(finding), finding.Severity switch {
            Severity.Error => DiagnosticSeverity.Error,
            Severity.Warning => DiagnosticSeverity.Warning,
            _ => DiagnosticSeverity.Info,
        }, additionalLocations: null, properties: null, finding.Text);

    bool IsSure(Finding finding) => finding.Code != RuleCodes.Count || app.Snapshots.Count == 1 && app.Problems.Count == 0;

    bool UsesCurrentData(AppType type) => ReferenceCheck.TargetsOf(type)
        .Select(app.Find).OfType<AppType>().Where(other => other.Project != facts.Name).All(freshness.IsCurrent);
}
