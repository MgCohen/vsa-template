namespace Umpire.Architecture;

internal static class Statements {
    public static IReadOnlyList<CountStatement> Counts(Architecture architecture, RuleBook book) =>
        [.. book.Counts.Select(count => Of(architecture, count)), .. Required(architecture)];

    public static IReadOnlyList<MatchStatement> Matches(Architecture architecture, RuleBook book) =>
        [.. book.Matches.Select(match => Of(architecture, match))];

    static CountStatement Of(Architecture architecture, Count count) {
        var text = count.ToString();
        Validate(architecture, count.Subjects, text);
        if (!count.HasLimit) {
            throw new InvalidOperationException(
                $"Rules: \"{text}\" has no limit. Add AtLeast, AtMost, Exactly or OfOneKind.");
        }
        RefuseNoFamily(architecture, count.Counted, text);
        var counted = count.Counted;
        return new CountStatement(text, Subjects(architecture, count.Subjects), false,
            b => count.Depth == Depth.Self ? b.Part.Is(counted) : b.Part.InFamily(counted),
            $"{Concept.Display(counted)} types", count.Min, count.Max, count.OneKind);
    }

    static MatchStatement Of(Architecture architecture, Match match) {
        var text = match.ToString();
        Validate(architecture, match.Subjects, text);
        RefuseNoFamily(architecture, match.Taken, text);
        if (match.Group is { } group && !architecture.Parts().Any(p => p.Is(group) && p.IsGroup)) {
            throw new InvalidOperationException($"Rules: \"{text}\": InSame<{Concept.Display(group)}> needs a concept that AddGroup adds.");
        }
        return new MatchStatement(text, Subjects(architecture, match.Subjects), match.Taken, match.Group);
    }

    static IEnumerable<CountStatement> Required(Architecture architecture) {
        foreach (var part in architecture.Parts().Where(p => !p.IsExternal)) {
            foreach (var type in part.Types.Where(t => (part.RequiredOf(t) ?? architecture.Options.Required) == true)) {
                var group = part.Groups.FirstOrDefault();
                var name = type.Name.Split('`')[0];
                var counted = $"{part.Name} types with base {name}";
                var text = $"{part.Name} requires {name} in each {group?.Name ?? "app"}";
                yield return new CountStatement(text, group is null ? [] : [group], group is null,
                    b => b.Part == part && b.Type.Is(type), counted, 1, null, false);
            }
        }
    }

    static IReadOnlyList<Part> Subjects(Architecture architecture, IReadOnlyList<Entry> entries) => [.. entries
        .SelectMany(e => architecture.Parts().Where(p => p.Is(e.Target) && !p.IsExternal && e.Filters.All(f => p.Under(f.Target))))
        .Distinct()];

    static void Validate(Architecture architecture, IReadOnlyList<Entry> entries, string text) {
        foreach (var entry in entries) {
            architecture.Validate(entry, text);
        }
    }

    static void RefuseNoFamily(Architecture architecture, Type concept, string text) {
        if (!architecture.Parts().Any(p => !p.IsExternal && p.InFamily(concept))) {
            throw new InvalidOperationException(
                $"Rules: \"{text}\" names {Concept.Display(concept)}, but the definition has no part of that concept in the app.");
        }
    }
}
