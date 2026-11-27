namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;
using Operations.Domain.Shared;
using Umpire.Config;

public sealed class CodeRun {
    const string ArchitectureKey = "umpire.architecture";

    readonly IReadOnlyList<RunningRule> rules;

    CodeRun(IReadOnlyList<RunningRule> rules, IReadOnlyList<Finding> notices) {
        this.rules = rules;
        Notices = notices;
    }

    public IReadOnlyList<Finding> Notices { get; }

    public bool ChecksTypes => rules.Any(running => running.Rule.ChecksTypes);

    public static CodeRun Start(CompileProject project, Func<string?, string, bool> selects) => Start(project, selects, CodeRule.All);

    public static CodeRun Start(CompileProject project, Func<string?, string, bool> selects, IReadOnlyList<CodeRule> all) {
        CodeConfig config;
        try {
            config = CodeConfig.Parse(project.ConfigText);
        } catch (ConfigFormatException e) {
            return new CodeRun([], [CodeRule.ConfigNotice.Finding(e.Message)]);
        }
        var architecture = config.Value(ArchitectureKey);
        var packs = new Dictionary<string, bool>(StringComparer.Ordinal);
        var running = all
            .Where(rule => rule.PackNamespace is not { } pack || Selected(packs, pack, () => selects(architecture, pack)))
            .Where(rule => !(project.IsFrameworkCode && rule.IsModuleRule) && IsOn(rule, config, project.SuppressedRules))
            .ToList();
        var notices = new List<Finding>();
        var valid = new List<RunningRule>();
        foreach (var rule in running) {
            var options = new RuleOptions(rule.Id, config, rule.Options);
            var invalid = Invalid(rule, options).ToList();
            if (invalid.Count == 0) {
                valid.Add(new RunningRule(rule, options, RuleSeverity.Of(RuleSeverity.Text(config, rule.Id)) ?? rule.Severity));
            }
            notices.AddRange(invalid);
        }
        return new CodeRun(valid, notices);
    }

    public IReadOnlyList<Finding> Check(CodeFile file) =>
        [.. rules.SelectMany(running => running.Rule.Check(file, running.Options).Select(running.Finding))];

    public IReadOnlyList<Finding> Check(CodeType type) =>
        [.. rules.SelectMany(running => running.Rule.Check(type, running.Options).Select(running.Finding))];

    static bool Selected(Dictionary<string, bool> packs, string pack, Func<bool> selects) {
        if (!packs.TryGetValue(pack, out var selected)) {
            selected = selects();
            packs.Add(pack, selected);
        }
        return selected;
    }

    static bool IsOn(CodeRule rule, CodeConfig config, IReadOnlyList<string> suppressed) {
        var severity = RuleSeverity.Text(config, rule.Id);
        if (RuleSeverity.IsOff(severity) || (severity is null && !rule.IsOnByDefault)) {
            return false;
        }
        return !suppressed.Contains(rule.Id, StringComparer.Ordinal);
    }

    static IEnumerable<Finding> Invalid(CodeRule rule, RuleOptions options) {
        foreach (var option in rule.Options) {
            var value = options.Configured(option.Key);
            if (!option.IsValid(value)) {
                yield return CodeRule.OptionNotice.Finding(rule.Id, option.Key, option.Kind, value ?? "missing");
            }
        }
    }

    sealed class RunningRule(CodeRule rule, RuleOptions options, FindingSeverity severity) {
        public CodeRule Rule { get; } = rule;

        public RuleOptions Options { get; } = options;

        public Finding Finding(CodeFinding found) =>
            new(Rule.Id, severity, found.Text, found.Place.File, found.Place.Line, found.Place.Column, found.Place.Length);
    }
}
