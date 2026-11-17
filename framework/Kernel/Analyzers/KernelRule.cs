namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;

public abstract class KernelRule : UmpireAnalyzerRule {
    public sealed override int Group => 1;

    public override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;

    public override bool IsModuleRule => false;
}
