using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The base of each analyzer DLL. It runs each VsaAnalyzerRule of the assembly of the derived class,
// so each DLL has one analyzer class that inherits RuleAnalyzer.
// Roslyn skips an analyzer only when all of its IDs are off, so this class skips each rule that is off by itself.
public abstract class RuleAnalyzer : DiagnosticAnalyzer {
    // VSA1000: an option of a rule is missing or not valid. Each value is in VSA.Rules.globalconfig, and no rule has a default.
    public static readonly DiagnosticDescriptor OptionDescriptor = new(
        "VSA1000",
        "A VSA rule option is missing or not valid",
        "Rule {0} needs the option vsa.{0}.{1}, {2}. The value is '{3}'. Set it in VSA.Rules.globalconfig or .editorconfig. "
            + "{0} does not run until the option is valid.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    protected RuleAnalyzer() {
        Rules = [.. GetType().Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(VsaAnalyzerRule)) && !t.IsAbstract)
            .Select(t => (VsaAnalyzerRule)Activator.CreateInstance(t)!)
            .OrderBy(r => r.Id, StringComparer.Ordinal)];
        SupportedDiagnostics = [OptionDescriptor, .. Rules.Select(r => r.Rule)];
    }

    // Each non-abstract VsaAnalyzerRule of the assembly of the derived class, in ID order.
    public ImmutableArray<VsaAnalyzerRule> Rules { get; }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public sealed override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            var global = start.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            var frameworkCode = global.TryGetValue("build_property.VsaFrameworkCode", out var value)
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            var missing = new List<Diagnostic>();
            var running = Rules.Where(r => !(frameworkCode && r.IsModuleRule) && !IsOff(r.Rule, start.Compilation, start.CancellationToken));
            foreach (var rule in running) {
                var invalid = Invalid(rule, global).ToList();
                if (invalid.Count == 0) {
                    rule.Register(start);
                }
                missing.AddRange(invalid);
            }
            if (missing.Count > 0) {
                start.RegisterCompilationEndAction(end => missing.ForEach(end.ReportDiagnostic));
            }
        });
    }

    // One VSA1000 diagnostic for each option of the rule that the global config does not give, or gives with a bad value.
    static IEnumerable<Diagnostic> Invalid(VsaAnalyzerRule rule, AnalyzerConfigOptions global) {
        var options = new VsaOptions(rule.Id, global);
        return rule.RequiredOptions
            .Select(option => (Option: option, Value: options.Value(option.Key)))
            .Where(pair => !pair.Option.IsValid(pair.Value))
            .Select(pair => Diagnostic.Create(OptionDescriptor, Location.None,
                rule.Id, pair.Option.Key, pair.Option.Kind, pair.Value ?? "missing"));
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
