namespace Umpire.Vsa.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class RuleAnalyzer : DiagnosticAnalyzer {
    const string ConfigFormatException = "Umpire.Config.ConfigFormatException";

    public static readonly DiagnosticDescriptor OptionDescriptor = new(
        "UMP010001",
        "An Umpire rule option is missing or not valid",
        "Rule {0} needs the option umpire.{0}.{1}, {2}. The value is '{3}'. Set it in the .umpireconfig file of the block. "
            + "{0} does not run until the option is valid.",
        "Umpire",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor ConfigDescriptor = new(
        "UMP010002",
        "The .umpireconfig file is not valid TOML",
        "The .umpireconfig file is not valid TOML: {0} Correct the file. The Umpire rules do not run until the file is valid.",
        "Umpire",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor LoadDescriptor = new(
        "UMP010003",
        "The Umpire analyzers cannot load a library",
        "The Umpire analyzers cannot load a library: {0} The analyzer DLL embeds Umpire.Config. Use a "
            + "compiler on .NET 8 or later. The Umpire rules do not run until the library loads.",
        "Umpire",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    protected RuleAnalyzer() {
        Rules = [.. GetType().Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(UmpireAnalyzerRule)) && !t.IsAbstract)
            .Select(t => (UmpireAnalyzerRule)Activator.CreateInstance(t)!)
            .OrderBy(r => r.Id, StringComparer.Ordinal)];
        SupportedDiagnostics = [OptionDescriptor, ConfigDescriptor, LoadDescriptor, .. Rules.Select(r => r.Rule)];
    }

    public ImmutableArray<UmpireAnalyzerRule> Rules { get; }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    protected virtual string? PackNamespace => null;

    public sealed override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    void Start(CompilationStartAnalysisContext start) {
        try {
            Run(start);
        } catch (Exception e) when (IsLoadFailure(e)) {
            if (Broken(LoadDescriptor, e.Message) is { } broken) {
                start.RegisterCompilationEndAction(end => end.ReportDiagnostic(broken));
            }
        }
    }

    void Run(CompilationStartAnalysisContext start) {
        if (Config(start.Options, start.CancellationToken, out var error) is not { } config) {
            if (Broken(ConfigDescriptor, error) is { } broken) {
                start.RegisterCompilationEndAction(end => end.ReportDiagnostic(broken));
            }
            return;
        }
        var architecture = config.Value(PackSelection.ArchitectureKey);
        if (PackNamespace is { } pack && !PackSelection.Selects(start.Compilation, architecture, pack)) {
            return;
        }
        var global = start.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        var frameworkCode = global.TryGetValue("build_property.UmpireFrameworkCode", out var value)
            && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        var running = Rules.Where(r => !(frameworkCode && r.IsModuleRule) && IsOn(r, config, start.Compilation, start.CancellationToken)).ToList();
        RuleSeverity.Use(start.Compilation, config, running);
        var missing = new List<Diagnostic>();
        foreach (var rule in running) {
            var invalid = Invalid(rule, config).ToList();
            if (invalid.Count == 0) {
                rule.Register(start);
            }
            missing.AddRange(invalid);
        }
        if (missing.Count > 0) {
            start.RegisterCompilationEndAction(end => missing.ForEach(end.ReportDiagnostic));
        }
    }

    static ConfigValues? Config(AnalyzerOptions options, System.Threading.CancellationToken ct, out string error) {
        error = "";
        try {
            return ConfigFile.Of(options, ct);
        } catch (Exception e) when (IsTomlError(e)) {
            error = e.Message;
            return null;
        }
    }

    Diagnostic? Broken(DiagnosticDescriptor descriptor, string message) =>
        PackNamespace is null ? Diagnostic.Create(descriptor, Location.None, message) : null;

    static bool IsTomlError(Exception e) {
        for (var type = e.GetType(); type is not null; type = type.BaseType) {
            if (type.FullName == ConfigFormatException) {
                return true;
            }
        }
        return false;
    }

    static bool IsLoadFailure(Exception e) =>
        e is System.IO.FileNotFoundException or System.IO.FileLoadException or BadImageFormatException or TypeLoadException
            or MissingMemberException;

    static IEnumerable<Diagnostic> Invalid(UmpireAnalyzerRule rule, ConfigValues config) {
        var options = new UmpireOptions(rule.Id, config, rule.Options);
        foreach (var option in rule.Options) {
            var value = options.Configured(option.Key);
            if (!option.IsValid(value)) {
                yield return Diagnostic.Create(OptionDescriptor, Location.None,
                    rule.Id, option.Key, option.Kind, value ?? "missing");
            }
        }
    }

    static bool IsOn(
        UmpireAnalyzerRule rule, ConfigValues config, Compilation compilation, System.Threading.CancellationToken ct) {
        var severity = RuleSeverity.Text(config, rule.Id);
        if (RuleSeverity.IsOff(severity) || (severity is null && !rule.IsOnByDefault)) {
            return false;
        }
        return !IsOff(rule.Rule, compilation, ct);
    }

    static bool IsOff(DiagnosticDescriptor rule, Compilation compilation, System.Threading.CancellationToken ct) {
        var provider = compilation.Options.SyntaxTreeOptionsProvider;
        var project = provider?.TryGetGlobalDiagnosticValue(rule.Id, ct, out var global) == true ? global
            : compilation.Options.SpecificDiagnosticOptions.TryGetValue(rule.Id, out var specific) ? specific
            : ReportDiagnostic.Default;
        return compilation.SyntaxTrees.All(tree =>
            (provider?.TryGetDiagnosticValue(tree, rule.Id, ct, out var file) == true ? file : project) == ReportDiagnostic.Suppress);
    }
}
