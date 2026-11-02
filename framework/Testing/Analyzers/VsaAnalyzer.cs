using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The base of each custom VSA analyzer. A derived analyzer gives one constant descriptor and registers its actions.
// Severity uses the standard key: dotnet_diagnostic.VSA1001.severity = warning.
// Options come from .editorconfig or .globalconfig. A rule key wins over a global key:
//   vsa.VSA1001.check_responses = false    only VSA1001
//   vsa.check_responses = false            all VSA rules that read the key
public abstract class VsaAnalyzer(DiagnosticDescriptor rule) : DiagnosticAnalyzer {
    public DiagnosticDescriptor Rule { get; } = rule;
    public string Id => Rule.Id;

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public sealed override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        Register(context);
    }

    protected abstract void Register(AnalysisContext context);

    // The options of one file. Without a file, only the global options (.globalconfig and MSBuild properties).
    protected VsaOptions Options(AnalyzerOptions options, SyntaxTree? tree = null) =>
        new(Id, tree is null ? options.AnalyzerConfigOptionsProvider.GlobalOptions : options.AnalyzerConfigOptionsProvider.GetOptions(tree));

    protected Diagnostic Create(Location location, params object[] arguments) => Diagnostic.Create(Rule, location, arguments);
}

public sealed class VsaOptions(string ruleId, AnalyzerConfigOptions options) {
    public string? Get(string key) =>
        options.TryGetValue($"vsa.{ruleId}.{key}", out var rule) ? rule
        : options.TryGetValue($"vsa.{key}", out var global) ? global
        : null;

    public bool Get(string key, bool fallback) => bool.TryParse(Get(key), out var value) ? value : fallback;
}
