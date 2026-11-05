using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The base of a rule that sets a maximum, for example the number of characters in a line.
// The limit is the option max: vsa.<rule ID>.max = <number>. A missing or bad value gives the default of the rule.
public abstract class LimitRule(DiagnosticDescriptor rule, int defaultMax) : VsaAnalyzerRule(rule) {
    public const string MaxKey = "max";

    public int DefaultMax { get; } = defaultMax;

    protected int Max(AnalyzerOptions options, SyntaxTree tree) => Options(options, tree).Get(MaxKey, DefaultMax);
}
