namespace Umpire.CheckModel.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CheckAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => CheckDescriptors.All;

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    static void Start(CompilationStartAnalysisContext context) {
        try {
            if (ProjectRun.Start(context.Compilation, context.Options, context.CancellationToken) is not { } run) {
                return;
            }
            context.RegisterSymbolAction(run.Check, SymbolKind.NamedType);
            context.RegisterCompilationEndAction(run.End);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException
            or ArgumentException or NotSupportedException) {
            var text = $"The check model cannot load: {e.Message}";
            context.RegisterCompilationEndAction(end => end.ReportDiagnostic(ProjectRun.DataDiagnostic(DataCodes.NoModel, text)));
        }
    }
}
