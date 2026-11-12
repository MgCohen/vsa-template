using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;

namespace VSA.Architecture.Engine;

public static class ArchRuleChecks {
    public static void Check(this IArchRule rule, AppArchitecture app) {
        var failed = rule.Evaluate(app.Architecture).Where(r => !r.Passed).ToList();
        if (failed.Count == 0) {
            return;
        }
        var details = failed.Select(r => r.EvaluatedObject is IType type ? Explain(app, type, r.Description) : r.Description);
        throw new ArchitectureException(string.Join(Environment.NewLine, details));
    }

    static string Explain(AppArchitecture app, IType type, string description) {
        var findings = app.Findings(type);
        return findings.Count > 0 ? string.Join(Environment.NewLine, findings) : $"{app.Location(type)}: error: {description}";
    }
}
