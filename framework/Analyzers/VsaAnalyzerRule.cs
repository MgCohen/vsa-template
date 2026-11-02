using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The base of each VSA analyzer rule. A rule gives one constant descriptor and registers its actions.
// VsaAnalyzer finds each rule of this assembly, so a new rule is one new class. No list of rules changes.
// Severity uses the standard key: dotnet_diagnostic.VSA1001.severity = warning.
// Options come from .editorconfig or .globalconfig. A rule key wins over a global key:
//   vsa.VSA1001.check_responses = false    only VSA1001
//   vsa.check_responses = false            all VSA rules that read the key
public abstract class VsaAnalyzerRule(DiagnosticDescriptor rule) {
    public DiagnosticDescriptor Rule { get; } = rule;
    public string Id => Rule.Id;

    // Runs at the start of each compilation in which the rule is not off.
    protected internal abstract void Register(CompilationStartAnalysisContext start);

    // The options of one file: its .editorconfig, the .globalconfig files and the MSBuild properties.
    protected VsaOptions Options(AnalyzerOptions options, SyntaxTree tree) =>
        new(Id, options.AnalyzerConfigOptionsProvider.GetOptions(tree));

    protected Diagnostic Create(Location location, params object[] arguments) => Diagnostic.Create(Rule, location, arguments);
}

public sealed class VsaOptions(string ruleId, AnalyzerConfigOptions options) {
    string? Get(string key) =>
        options.TryGetValue($"vsa.{ruleId}.{key}", out var rule) ? rule
        : options.TryGetValue($"vsa.{key}", out var global) ? global
        : null;

    public bool Get(string key, bool fallback) => bool.TryParse(Get(key), out var value) ? value : fallback;
}
