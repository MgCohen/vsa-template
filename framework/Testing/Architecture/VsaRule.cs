using System.Reflection;
using VSA.Testing.Architecture.Rules;

namespace VSA.Testing.Architecture;

// One architecture rule over the whole app. Check returns one violation for each problem, or none.
public interface IVsaRule {
    string Id { get; }
    string Title { get; }
    IEnumerable<Violation> Check(VsaApp app);
}

// Message: what is wrong, the types, and the fix.
public sealed record Violation(string RuleId, string Message) {
    public override string ToString() => $"{RuleId}: {Message}";
}

public static class VsaRules {
    public static IReadOnlyList<IVsaRule> All { get; } = [new OneHandlerPerRequest(), new UniqueRoutes(), new OneModuleClass()];

    // Throws one exception with all violations of the rule, one on each line.
    public static void Assert(IVsaRule rule, VsaApp app) {
        var violations = rule.Check(app).ToList();
        if (violations.Count > 0) {
            throw new ArchitectureRuleException($"{rule.Id} {rule.Title}:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
        }
    }

    // GetTypes fails if one type does not load. The rules use the types that load.
    internal static IEnumerable<Type> TypesOf(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    internal static Type? GenericBase(Type type, Type openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == openBase) {
                return b;
            }
        }
        return null;
    }
}

// A failed architecture rule. It is not a test framework type, so any test framework shows the message.
public sealed class ArchitectureRuleException(string message) : Exception(message);
