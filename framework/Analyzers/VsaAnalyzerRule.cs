using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The base of each VSA analyzer rule. A rule gives one constant descriptor and registers its actions.
// VsaAnalyzer finds each rule of this assembly, so a new rule is one new class. No list of rules changes.
// Severity uses the standard key: dotnet_diagnostic.VSA1001.severity = warning.
// Options come from VSA.Rules.globalconfig or .editorconfig. No rule has a default value: a rule lists its options in
// RequiredOptions, and VsaAnalyzer reports VSA1000 if one is missing or not valid. A rule key wins over a global key:
//   vsa.VSA1001.check_responses = false    only VSA1001
//   vsa.check_responses = false            all VSA rules that read the key
//   vsa.VSA1007.allowed_bases = A, B       a list, with a comma or a semicolon between the items
// In an .editorconfig or .globalconfig line, a semicolon starts a comment. Write a comma there.
public abstract class VsaAnalyzerRule(DiagnosticDescriptor rule) {
    public DiagnosticDescriptor Rule { get; } = rule;
    public string Id => Rule.Id;

    // True for a rule about the code of a module, for example the layers. False for a rule about any code, for example the format.
    // The framework code of the VSA repository gets only the rules about any code (MSBuild property VsaFrameworkCode = true).
    public virtual bool IsModuleRule => true;

    // The options that the rule reads. VsaAnalyzer checks them before Register.
    public virtual IReadOnlyList<VsaOption> RequiredOptions => [];

    // Runs at the start of each compilation in which the rule is not off and its options are valid.
    protected internal abstract void Register(CompilationStartAnalysisContext start);

    // The options of one file: its .editorconfig, the .globalconfig files and the MSBuild properties.
    protected VsaOptions Options(AnalyzerOptions options, SyntaxTree tree) =>
        new(Id, options.AnalyzerConfigOptionsProvider.GetOptions(tree));

    protected Diagnostic Create(Location location, params object[] arguments) => Diagnostic.Create(Rule, location, arguments);
}
