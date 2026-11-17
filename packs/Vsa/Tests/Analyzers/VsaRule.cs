namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;

public abstract class VsaRule : UmpireAnalyzerRule {
    public sealed override int Group => 2;

    public override DiagnosticSeverity Severity => DiagnosticSeverity.Error;
}
