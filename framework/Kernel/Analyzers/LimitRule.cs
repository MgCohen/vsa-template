namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class LimitRule : KernelRule {
    public const string MaxKey = "max";

    public override IReadOnlyList<UmpireOption> Options => [UmpireOption.Int(MaxKey, DefaultMax)];

    protected abstract int DefaultMax { get; }

    protected int? Max(AnalyzerOptions options, SyntaxTree tree) => Values(options, tree).Int(MaxKey);
}
