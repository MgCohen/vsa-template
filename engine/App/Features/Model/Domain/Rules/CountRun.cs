namespace Umpire.CheckModel;

sealed class CountRun {
    readonly CountRule rule;
    readonly IReadOnlyList<PlacedType> app;
    readonly IReadOnlyList<Part> subjects;
    readonly string text;

    public CountRun(AppModel model, CountRule rule, IReadOnlyList<PlacedType> app) {
        this.rule = rule;
        this.app = app;
        subjects = [.. rule.Parts.Select(p => model.Parts[p])];
        var part = rule.IsRequired ? model.Parts[rule.Counted.Parts[0]].Name : "";
        var type = rule.Counted.Type?.Name.Split('`')[0];
        Counts = rule.IsRequired ? $"{part} types with base {type}" : rule.Counts;
        text = rule.IsRequired
            ? $"{part} requires {type} in each {(subjects.Count > 0 ? subjects[0].Name : null) ?? "app"}"
            : rule.ToString();
    }

    string Counts { get; }

    public IEnumerable<CountFailure> Failures() {
        if (rule.IsWholeApp) {
            return app.Count == 0 ? [] : Check("The app", "app", Counted(app), First(app));
        }
        return subjects.SelectMany(s => s.IsGroup ? InGroup(s) : rule.IsPerPart ? InPart(s) : OfType(s));
    }

    static AppType First(IEnumerable<PlacedType> types) => types.Select(t => t.Type)
        .OrderBy(t => t.File, StringComparer.Ordinal)
        .ThenBy(t => t.Line)
        .ThenBy(t => t.Name.FullName, StringComparer.Ordinal)
        .First();

    IEnumerable<CountFailure> InGroup(Part subject) => app.Where(t => subject.Holds(t.Placement.Part))
        .GroupBy(t => t.Placement.InstanceOf(subject))
        .OrderBy(instance => instance.Key, StringComparer.Ordinal)
        .SelectMany(instance => Check($"{subject.Name} {instance.Key}", subject.Name, Counted(instance), First(instance)));

    IEnumerable<CountFailure> InPart(Part subject) {
        var inPart = app.Where(t => subject.Holds(t.Placement.Part)).ToList();
        if (inPart.Count > 0) {
            return Check(subject.Name, subject.Name, Counted(inPart), First(inPart));
        }
        return app.Count == 0 || rule.Min is not > 0 ? [] : Check(subject.Name, subject.Name, [], First(app));
    }

    IEnumerable<CountFailure> OfType(Part subject) {
        var candidates = Counted(app);
        return app.Where(t => t.Placement.Part == subject && !t.Placement.FollowsUser && !t.Type.Content.IsAbstract)
            .SelectMany(t => Check($"{t.Type.Name.Name} in {t.Placement.Where}", null,
                [.. candidates.Where(c => c.Type.Content.BaseArguments.Any(a => a.FullName == t.Type.Name.FullName))], t.Type));
    }

    List<PlacedType> Counted(IEnumerable<PlacedType> types) {
        var all = types.ToList();
        var taken = new HashSet<string>(rule.SkipsTaken ? all.SelectMany(t => t.Type.Content.BaseArguments).Select(a => a.FullName) : []);
        return [.. all.Where(t => !t.Placement.FollowsUser && !t.Type.Content.IsAbstract && !t.Type.IsNested
            && !taken.Contains(t.Type.Name.FullName) && IsCounted(t))];
    }

    bool IsCounted(PlacedType type) =>
        (rule.Counted.InAnyPart || rule.Counted.Parts.Contains(type.Placement.Part.Position))
        && (rule.Counted.Type is not { } counted || type.Type.Is(counted));

    IEnumerable<CountFailure> Check(string subject, string? group, List<PlacedType> found, AppType at) {
        var counted = group is null ? $"{Counts} that take it" : Counts;
        var names = found.Count == 0 ? "" : $" ({string.Join(", ", found.Select(f => f.Type.Name.Name))})";
        var needs = $"{subject} has {found.Count} {counted}{names}: the rule \"{text}\" needs {rule.Limits}. ";
        if (rule.Min is { } min && found.Count < min) {
            yield return new CountFailure(at, needs
                + (group is null ? $"Add one, or remove {at.Name.Name}." : "Add one, or move the other types to another part."));
        }
        if (rule.Max is { } max && found.Count > max) {
            yield return new CountFailure(at, needs
                + (group is null ? $"Keep {max}, and remove the others." : $"Move the others to another {group}."));
        }
        var kinds = found.Select(f => f.Placement.Part.Name).Distinct().Count();
        if (rule.OneKind && kinds > 1) {
            var byKind = string.Join(", ", found.Select(f => $"{f.Type.Name.Name} ({f.Placement.Part.Name})"));
            yield return new CountFailure(at,
                $"{subject} has {counted} of {kinds} kinds: {byKind}. The rule \"{text}\" needs one kind. "
                + (group is null ? "Keep one kind." : $"Move each kind to its own {group}."));
        }
    }
}
