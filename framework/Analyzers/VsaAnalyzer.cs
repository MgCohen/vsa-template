using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The one analyzer of VSA. It runs each VsaAnalyzerRule of this assembly.
// Roslyn skips an analyzer only when all of its IDs are off, so this class skips each rule that is off by itself.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VsaAnalyzer : DiagnosticAnalyzer {
    // Each non-abstract VsaAnalyzerRule of this assembly, in ID order.
    public static ImmutableArray<VsaAnalyzerRule> Rules { get; } = [.. typeof(VsaAnalyzer).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(VsaAnalyzerRule)) && !t.IsAbstract)
        .Select(t => (VsaAnalyzerRule)Activator.CreateInstance(t)!)
        .OrderBy(r => r.Id, StringComparer.Ordinal)];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [.. Rules.Select(r => r.Rule)];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            foreach (var rule in Rules.Where(r => !IsOff(r.Rule, start.Compilation, start.CancellationToken))) {
                rule.Register(start);
            }
        });
    }

    // A rule is off if its severity is none in each file: from .editorconfig, .globalconfig, a rule set or NoWarn.
    static bool IsOff(DiagnosticDescriptor rule, Compilation compilation, System.Threading.CancellationToken ct) {
        var provider = compilation.Options.SyntaxTreeOptionsProvider;
        // The global config wins over the project options. Roslyn also drops each diagnostic of a rule that is off,
        // so this check only prevents work, and if it is not sure, the rule runs.
        var project = provider?.TryGetGlobalDiagnosticValue(rule.Id, ct, out var global) == true ? global
            : compilation.Options.SpecificDiagnosticOptions.TryGetValue(rule.Id, out var specific) ? specific
            : rule.IsEnabledByDefault ? ReportDiagnostic.Default : ReportDiagnostic.Suppress;
        return compilation.SyntaxTrees.All(tree =>
            (provider?.TryGetDiagnosticValue(tree, rule.Id, ct, out var file) == true ? file : project) == ReportDiagnostic.Suppress);
    }
}
