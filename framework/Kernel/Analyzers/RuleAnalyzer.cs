namespace Umpire.Vsa.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class RuleAnalyzer : DiagnosticAnalyzer {
    public static readonly DiagnosticDescriptor OptionDescriptor = new(
        "UMP010001",
        "An Umpire rule option is missing or not valid",
        "Rule {0} needs the option umpire.{0}.{1}, {2}. The value is '{3}'. Set it in the globalconfig file of the analyzer package or in .umpireconfig. "
            + "{0} does not run until the option is valid.",
        "Umpire",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    protected RuleAnalyzer() {
        Rules = [.. GetType().Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(UmpireAnalyzerRule)) && !t.IsAbstract)
            .Select(t => (UmpireAnalyzerRule)Activator.CreateInstance(t)!)
            .OrderBy(r => r.Id, StringComparer.Ordinal)];
        SupportedDiagnostics = [OptionDescriptor, .. Rules.Select(r => r.Rule)];
    }

    public ImmutableArray<UmpireAnalyzerRule> Rules { get; }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    protected virtual string? PackAssembly => null;

    public sealed override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            var global = start.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (PackAssembly is { } pack && !PackSelection.Selects(start.Compilation, global, pack)) {
                return;
            }
            var frameworkCode = global.TryGetValue("build_property.UmpireFrameworkCode", out var value)
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

    static IEnumerable<Diagnostic> Invalid(UmpireAnalyzerRule rule, AnalyzerConfigOptions global) {
        var options = new UmpireOptions(rule.Id, global, rule.Options);
        return rule.Options
            .Select(option => (Option: option, Value: options.Configured(option.Key)))
            .Where(pair => !pair.Option.IsValid(pair.Value))
            .Select(pair => Diagnostic.Create(OptionDescriptor, Location.None,
                rule.Id, pair.Option.Key, pair.Option.Kind, pair.Value ?? "missing"));
    }

    static bool IsOff(DiagnosticDescriptor rule, Compilation compilation, System.Threading.CancellationToken ct) {
        var provider = compilation.Options.SyntaxTreeOptionsProvider;
        var project = provider?.TryGetGlobalDiagnosticValue(rule.Id, ct, out var global) == true ? global
            : compilation.Options.SpecificDiagnosticOptions.TryGetValue(rule.Id, out var specific) ? specific
            : rule.IsEnabledByDefault ? ReportDiagnostic.Default : ReportDiagnostic.Suppress;
        return compilation.SyntaxTrees.All(tree =>
            (provider?.TryGetDiagnosticValue(tree, rule.Id, ct, out var file) == true ? file : project) == ReportDiagnostic.Suppress);
    }
}
