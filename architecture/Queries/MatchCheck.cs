namespace Umpire.Architecture;

internal static class MatchCheck {
    public static IEnumerable<MatchResult> Failures(MatchStatement statement, Bindings bindings) {
        var taken = Concept.Display(statement.Taken);
        foreach (var subject in bindings.All.Where(b => !b.External && !b.FollowsUser && !b.Type.IsAbstract && statement.Subjects.Contains(b.Part))) {
            var arguments = subject.Type.BaseArguments.Select(bindings.Of).OfType<Binding>()
                .Where(b => !b.External && b.Part.InFamily(statement.Taken)).Distinct().ToList();
            if (arguments.Count == 0) {
                yield return new MatchResult(statement.Rule, subject, null,
                    $"{subject.Type.Name} in {subject.Where} takes no {taken} type: the rule \"{statement.Rule}\" needs one. "
                    + $"Give its base type a {taken} type argument, or move it.");
                continue;
            }
            if (statement.Group is not { } groupConcept) {
                continue;
            }
            var group = Concept.Display(groupConcept);
            var own = subject.Part.Groups.FirstOrDefault(g => g.Is(groupConcept));
            foreach (var other in arguments.Where(a => own is null || !SameInstance(own, subject, a))) {
                yield return new MatchResult(statement.Rule, subject, other,
                    $"{subject.Type.Name} in {subject.Where} takes {other.Type.Name} in {other.Where}: "
                    + $"the rule \"{statement.Rule}\" needs both in one {group}. "
                    + $"Move {subject.Type.Name} to the {group} of {other.Type.Name}, or take a type of its own {group}.");
            }
        }
    }

    static bool SameInstance(Part group, Binding subject, Binding other) =>
        group.Holds(other.Part) && group.Groups.All(g => subject.Values.GetValueOrDefault(g.Name) == other.Values.GetValueOrDefault(g.Name));
}
