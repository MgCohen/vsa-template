namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class LimitRule(DiagnosticDescriptor rule) : UmpireAnalyzerRule(rule) {
    public const string MaxKey = "max";

    public override bool IsModuleRule => false;

    public override IReadOnlyList<UmpireOption> RequiredOptions { get; } = [UmpireOption.Int(MaxKey)];

    protected int? Max(AnalyzerOptions options, SyntaxTree tree) => Options(options, tree).Int(MaxKey);
}
