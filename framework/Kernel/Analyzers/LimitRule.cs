using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Umpire.Vsa.Analyzers;

// The base of a rule that sets a maximum, for example the number of characters in a line.
// The limit is the option max in Umpire.Rules.globalconfig: umpire.<rule ID>.max = <number>.
public abstract class LimitRule(DiagnosticDescriptor rule) : UmpireAnalyzerRule(rule) {
    public const string MaxKey = "max";

    public override bool IsModuleRule => false;

    public override IReadOnlyList<UmpireOption> RequiredOptions { get; } = [UmpireOption.Int(MaxKey)];

    // Null if the value of this file is not valid. Then the rule does not check the file.
    protected int? Max(AnalyzerOptions options, SyntaxTree tree) => Options(options, tree).Int(MaxKey);
}
