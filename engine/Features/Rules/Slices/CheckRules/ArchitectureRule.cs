namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;
using Umpire.Architecture;

abstract class ArchitectureRule {
    const string Area = "02";

    static readonly IReadOnlyList<ArchitectureRule> Rules = [.. typeof(ArchitectureRule).Assembly.GetTypes()
        .Where(type => type.IsSubclassOf(typeof(ArchitectureRule)) && !type.IsAbstract)
        .Select(type => (ArchitectureRule)Activator.CreateInstance(type)!)
        .OrderBy(rule => rule.Code, StringComparer.Ordinal)];

    public static IReadOnlyList<ArchitectureRule> All => Rules;

    public abstract int Group { get; }

    public abstract int Number { get; }

    public string Code => $"UMP{Area}{Group:00}{Number:00}";

    public virtual FindingSeverity Severity => FindingSeverity.Error;

    public static TRule Of<TRule>() where TRule : ArchitectureRule => Rules.OfType<TRule>().Single();

    public IReadOnlyList<Finding> Check(ArchitectureCheck check, RuleConfig config) {
        var values = config.For(Code);
        return values.Severity(Severity) is { } severity
            ? [.. Find(check, values).Select(finding => finding with { Severity = severity })]
            : [];
    }

    protected abstract IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values);

    protected Finding At(Source source, string text) => new(Code, Severity, text, source.File, source.Line, source.Column);

    protected Finding At(string? file, string text) => new(Code, Severity, text, file);
}
