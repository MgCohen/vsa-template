namespace Umpire.Architecture;

internal static class CountCheck {
    public static IEnumerable<CountResult> Failures(CountStatement statement, Bindings bindings) {
        var app = bindings.All.Where(b => !b.External).ToList();
        if (statement.WholeApp) {
            return app.Count == 0 ? [] : Check(statement, "The app", "app", Counted(statement, app), First(app));
        }
        return statement.Subjects.SelectMany(subject => subject.IsGroup
            ? GroupFailures(statement, subject, app)
            : statement.PerPart ? PartFailures(statement, subject, app) : TypeFailures(statement, subject, app));
    }

    static IEnumerable<CountResult> GroupFailures(CountStatement statement, Part subject, List<Binding> app) =>
        app.Where(b => subject.Holds(b.Part))
            .GroupBy(b => InstancePath(subject, b))
            .OrderBy(instance => instance.Key, StringComparer.Ordinal)
            .SelectMany(instance => Check(statement, $"{subject.Name} {instance.Key}", subject.Name,
                Counted(statement, instance), First(instance)));

    static IEnumerable<CountResult> PartFailures(CountStatement statement, Part subject, List<Binding> app) {
        var inPart = app.Where(b => subject.Holds(b.Part)).ToList();
        if (inPart.Count == 0) {
            return app.Count == 0 || statement.Min is not > 0
                ? []
                : Check(statement, subject.Name, subject.Name, [], First(app));
        }
        return Check(statement, subject.Name, subject.Name, Counted(statement, inPart), First(inPart));
    }

    static IEnumerable<CountResult> TypeFailures(CountStatement statement, Part subject, List<Binding> app) {
        var candidates = Counted(statement, app);
        return app.Where(b => b.Part == subject && !b.FollowsUser && !b.Type.IsAbstract)
            .SelectMany(type => Check(statement, $"{type.Type.Name} in {type.Where}", null,
                [.. candidates.Where(c => c.Type.BaseArguments.Contains(type.Type.FullName))], type));
    }

    static List<Binding> Counted(CountStatement statement, IEnumerable<Binding> bindings) {
        var all = bindings.ToList();
        var taken = statement.SkipTaken ? new HashSet<string>(all.SelectMany(b => b.Type.BaseArguments)) : [];
        return [.. all.Where(b => !b.FollowsUser && !b.Type.IsAbstract && !b.Type.IsNested
            && !taken.Contains(b.Type.FullName) && statement.Counts(b))];
    }

    static IEnumerable<CountResult> Check(CountStatement statement, string subject, string? group, List<Binding> found, Binding at) {
        var counted = group is null ? $"{statement.Counted} that take it" : statement.Counted;
        var names = found.Count == 0 ? "" : $" ({string.Join(", ", found.Select(f => f.Type.Name))})";
        var need = Limits.Need(statement.Min, statement.Max, false);
        if (statement.Min is { } min && found.Count < min) {
            yield return new CountResult(statement.Rule, CountProblem.TooFew, subject, found, at,
                $"{subject} has {found.Count} {counted}{names}: the rule \"{statement.Rule}\" needs {need}. "
                + (group is null ? $"Add one, or remove {at.Type.Name}." : "Add one, or move the other types to another part."));
        }
        if (statement.Max is { } max && found.Count > max) {
            yield return new CountResult(statement.Rule, CountProblem.TooMany, subject, found, max == 0 ? First(found) : at,
                $"{subject} has {found.Count} {counted}{names}: the rule \"{statement.Rule}\" needs {need}. "
                + (max == 0 ? $"Remove {string.Join(", ", found.Select(f => f.Type.Name))}, or move each one to a part where the rule does not apply."
                    : group is null ? $"Keep {max}, and remove the others." : $"Move the others to another {group}."));
        }
        var kinds = found.Select(b => b.Part.Name).Distinct().ToList();
        if (statement.OneKind && kinds.Count > 1) {
            var byKind = string.Join(", ", found.Select(f => $"{f.Type.Name} ({f.Part.Name})"));
            yield return new CountResult(statement.Rule, CountProblem.ManyKinds, subject, found, at,
                $"{subject} has {counted} of {kinds.Count} kinds: {byKind}. The rule \"{statement.Rule}\" needs one kind. "
                + (group is null ? "Keep one kind." : $"Move each kind to its own {group}."));
        }
    }

    static Binding First(IEnumerable<Binding> bindings) => bindings
        .OrderBy(b => b.Source?.File, StringComparer.Ordinal)
        .ThenBy(b => b.Source?.Line)
        .ThenBy(b => b.Type.FullName, StringComparer.Ordinal)
        .First();

    static string InstancePath(Part subject, Binding binding) =>
        binding.Values.Aggregate(subject.Path, (path, value) => path.Replace($"{{{value.Key.ToLowerInvariant()}}}", value.Value));
}
