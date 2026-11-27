namespace UmpireEngine;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EngineAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Descriptors.All;

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    static void Start(CompilationStartAnalysisContext context) {
        CompileRun run;
        try {
            run = CompileRun.Start(context.Compilation, context.Options, context.CancellationToken);
        } catch (Exception e) when (CompileApp.IsFailure(e)) {
            var failure = CompileApp.IsLoadFailure(e)
                ? Descriptors.LoadFailure(CompileApp.Reason(e))
                : Descriptors.Failure($"The compile job cannot start: {CompileApp.Reason(e)}");
            context.RegisterCompilationEndAction(end => end.ReportDiagnostic(failure));
            return;
        }
        context.RegisterSemanticModelAction(run.File);
        context.RegisterSymbolAction(run.Type, SymbolKind.NamedType);
        context.RegisterCompilationEndAction(run.End);
    }
}
