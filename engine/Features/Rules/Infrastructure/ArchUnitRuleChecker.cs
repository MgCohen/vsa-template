using System.Reflection;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace Rules.Infrastructure;

public static class ArchUnitRuleChecker {
    public static RuleCheck Check(ArchitectureDefinition definition, IReadOnlyList<Assembly> assemblies, string appRoot) {
        var map = ArchitectureMap.Load(definition, assemblies, appRoot);
        return new RuleCheck([.. map.Findings().Select(ToFinding)], map.Bindings.AppTypes.Count);
    }

    static RuleCheckFinding ToFinding(RuleFinding finding) => new(
        finding.Code,
        finding.Severity == RuleSeverity.Warning,
        finding.File,
        finding.File is null ? null : finding.Line,
        finding.Text);
}
