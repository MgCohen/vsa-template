using System.Reflection;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace Rules.Infrastructure;

public static class ArchUnitRuleChecker {
    public static RuleCheck Check(ArchitectureDefinition definition, IReadOnlyList<Assembly> assemblies, string appRoot) {
        var map = ArchitectureMap.Load(definition, assemblies, appRoot);
        return new RuleCheck([.. map.AppTypes.SelectMany(map.Findings)], map.AppTypes.Count, map.AppRoot);
    }
}
