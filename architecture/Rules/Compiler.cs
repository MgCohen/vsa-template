namespace Umpire.Architecture;

internal sealed class Compiler {
    const string SameConceptText = "same concept";
    readonly RuleBook book;
    readonly Dictionary<Part, Options> partOptions = [];

    internal Compiler(Architecture architecture) {
        Options = Options.From(architecture.ConfigureDefaults).Over(Options.BuiltIn);
        Block = new Block();
        architecture.DefineBlock(Block);
        Parts = [.. Block.Walk().Skip(1)];
        CheckShared();
        foreach (var part in Parts) {
            partOptions[part] = part.Options.Over(Options);
        }
        book = new RuleBook(Block);
        foreach (var concept in Parts.Select(p => p.Concept!).GroupBy(c => c.GetType()).Select(g => g.First())) {
            concept.AddRules(book.Of(concept.GetType()));
        }
        architecture.AddRules(book);
        UseRules = CompileUseRules();
        Counts = [.. book.Counts.Select(Of), .. Required()];
        Matches = [.. book.Matches.Select(Of)];
        Traits = [.. book.Traits.Select(Of)];
        Places = [.. book.Places.Select(Of)];
        CheckNames();
    }

    public Options Options { get; }
    public Block Block { get; }
    public IReadOnlyList<Part> Parts { get; }
    public IReadOnlyDictionary<Part, Options> PartOptions => partOptions;
    public IReadOnlyList<PartRule> UseRules { get; }
    public IReadOnlyList<CountStatement> Counts { get; }
    public IReadOnlyList<MatchStatement> Matches { get; }
    public IReadOnlyList<TraitStatement> Traits { get; }
    public IReadOnlyList<PlaceStatement> Places { get; }

    static IEnumerable<Part> Expand(Part part, Depth depth) => depth switch {
        Depth.Self => [part],
        Depth.WithChildren => part.Walk(),
        _ => throw new InvalidOperationException($"Depth {depth} has no expansion. Add it to Expand."),
    };

    static bool PassesFilters(Part part, Entry entry) => entry.Filters.All(f => part.Under(f.Target));

    static Condition? ConditionOf(Part from, Part to, Entry entry) {
        var condition = Condition.Always;
        foreach (var filter in entry.Filters) {
            switch (filter.Kind) {
                case FilterKind.Under:
                    if (!to.Under(filter.Target)) {
                        return null;
                    }
                    break;
                case FilterKind.InSame:
                    var own = to.Groups.FirstOrDefault(i => i.Is(filter.Target));
                    if (own is null || !own.Holds(from)) {
                        return null;
                    }
                    condition = condition.AndSame(own);
                    break;
                case FilterKind.InOther:
                    var other = to.Groups.FirstOrDefault(i => i.Is(filter.Target));
                    if (other is null || !other.Parent!.Holds(from)) {
                        return null;
                    }
                    condition = other.Ancestors().Where(a => a.IsGroup).Reverse()
                        .Aggregate(condition, (current, group) => current.AndSame(group)).AndOther(other);
                    break;
            }
        }
        return condition;
    }

    Options OptionsOf(Part part) => partOptions.TryGetValue(part, out var options) ? options : Options;

    void CheckShared() {
        var groups = Parts.Where(p => !p.HasFolder && !p.IsGroup).GroupBy(p => p.Parent).Where(g => g.Count() > 1);
        foreach (var shared in groups) {
            var parts = shared.ToList();
            var none = parts.FirstOrDefault(p => p.Types.Count == 0);
            if (none is not null) {
                throw new InvalidOperationException(
                    $"Define: {none.Name} shares the folder \"{shared.Key!.Path}\" with "
                    + $"{string.Join(", ", parts.Where(p => p != none).Select(p => p.Name))}, but it has no OfType. "
                    + "Add OfType<T>() to each part in a shared folder.");
            }
            var twice = parts.SelectMany(p => p.Types.Select(t => new { Part = p, Type = t })).GroupBy(x => x.Type).FirstOrDefault(g => g.Count() > 1);
            if (twice is not null) {
                throw new InvalidOperationException(
                    $"Define: {string.Join(" and ", twice.Select(x => x.Part.Name))} share the folder \"{shared.Key!.Path}\" "
                    + $"and have the same OfType<{twice.Key.Name}>. Give each part a different type.");
            }
        }
    }

    List<PartRule> CompileUseRules() {
        var all = new List<PartRule>();
        AddSameConcept(all);
        foreach (var rule in book.Rules) {
            AddRule(all, rule);
        }
        return all;
    }

    void AddSameConcept(List<PartRule> all) {
        foreach (var from in Parts) {
            foreach (var to in Parts.Where(t => t.Concept!.GetType() == from.Concept!.GetType() && OptionsOf(t).UseSameConcept == true)) {
                all.Add(new PartRule(from, to, Boundaries(from, to, Condition.Always, null), Permission.Allow, SameConceptText, OptionsOf(to).Access!.Value));
            }
        }
    }

    void AddRule(List<PartRule> all, Rule rule) {
        var text = rule.ToString();
        Validate(rule.From.Concat(rule.Targets), text);
        var fromParts = rule.From.SelectMany(e => Branch(e).Where(p => PassesFilters(p, e))).Distinct().ToList();
        foreach (var entry in rule.Targets) {
            var matched = false;
            foreach (var from in fromParts) {
                foreach (var to in Branch(entry)) {
                    if (ConditionOf(from, to, entry) is not { } condition) {
                        continue;
                    }
                    if (rule.Permission == Permission.Allow) {
                        condition = Boundaries(from, to, condition, entry);
                    }
                    all.Add(new PartRule(from, to, condition, rule.Permission, text, entry.Access ?? OptionsOf(to).Access!.Value));
                    matched = true;
                }
            }
            if (!matched) {
                throw new InvalidOperationException(
                    $"Rules: \"{text}\": the target {entry} matches no part. Change the target or remove the rule.");
            }
        }
    }

    IEnumerable<Part> Branch(Entry entry) =>
        Parts.Where(p => p.Is(entry.Target)).SelectMany(p => Expand(p, entry.Depth ?? OptionsOf(p).Depth!.Value)).Distinct();

    Condition Boundaries(Part from, Part to, Condition condition, Entry? entry) {
        if (entry?.Boundary == Boundary.Open) {
            return condition;
        }
        foreach (var group in to.Groups.Reverse()) {
            if (entry?.Filters.Any(f => f.Kind == FilterKind.InOther && group.Is(f.Target)) == true) {
                break;
            }
            if (OptionsOf(group).Boundary == Boundary.Closed && from.Groups.Contains(group)) {
                condition = condition.AndSame(group);
            }
        }
        return condition;
    }

    CountStatement Of(Count count) {
        var text = count.ToString();
        Validate(count.Subjects, text);
        if (!count.HasLimit) {
            throw new InvalidOperationException(
                $"Rules: \"{text}\" has no limit. Add AtLeast, AtMost, Exactly or OfOneKind.");
        }
        RefuseNoFamily(count.Counted, text);
        var counted = count.Counted;
        return new CountStatement(text, Subjects(count.Subjects, text), false,
            b => count.Depth == Depth.Self ? b.Part.Is(counted) : b.Part.InFamily(counted),
            $"{Concept.Display(counted)} types", count.Min, count.Max, count.OneKind);
    }

    MatchStatement Of(Match match) {
        var text = match.ToString();
        Validate(match.Subjects, text);
        RefuseNoFamily(match.Taken, text);
        if (match.Group is { } group) {
            RefuseNoGroup(group, $"InSame<{Concept.Display(group)}>", text);
        }
        return new MatchStatement(text, Subjects(match.Subjects, text), match.Taken, match.Group);
    }

    TraitStatement Of(TraitRule rule) {
        var text = rule.ToString();
        Validate(rule.Subjects, text);
        return new TraitStatement(text, Subjects(rule.Subjects, text), rule.Condition);
    }

    PlaceStatement Of(PlaceLimit limit) {
        var text = limit.ToString();
        var places = limit.Places;
        Validate(places, text);
        return new PlaceStatement(text, Subjects(places, text), limit.Type);
    }

    void CheckNames() {
        foreach (var part in Parts.Where(p => p.NameRule is { OfAGroup: true } && p.NameRule.GroupOf(p) is null)) {
            var group = part.NameRule!.Group is { } concept ? Concept.Display(concept) : "group";
            throw new InvalidOperationException(
                $"Define: {part.Name} has the name rule \"{part.NameRule}\", but it is not under a {group}. "
                + "Add the part under that group, or use another name rule.");
        }
    }

    IEnumerable<CountStatement> Required() {
        foreach (var part in Parts.Where(p => !p.IsExternal)) {
            foreach (var type in part.Types.Where(t => (part.RequiredOf(t) ?? OptionsOf(part).Required) == true)) {
                var group = part.Groups.FirstOrDefault();
                var name = type.Name.Split('`')[0];
                var counted = $"{part.Name} types with base {name}";
                var text = $"{part.Name} requires {name} in each {group?.Name ?? "app"}";
                yield return new CountStatement(text, group is null ? [] : [group], group is null,
                    b => b.Part == part && b.Type.Is(type), counted, 1, null, false);
            }
        }
    }

    IReadOnlyList<Part> Subjects(IReadOnlyList<Entry> entries, string text) {
        List<Part> subjects = [.. entries
            .SelectMany(e => Parts.Where(p => p.Is(e.Target) && !p.IsExternal && PassesFilters(p, e)))
            .Distinct()];
        if (subjects.Count == 0) {
            throw new InvalidOperationException(
                $"Rules: \"{text}\": the subject matches no part of the app. Change the subject or its filter, or remove the rule.");
        }
        return subjects;
    }

    void Validate(IEnumerable<Entry> entries, string text) {
        foreach (var entry in entries) {
            foreach (var target in entry.Filters.Select(f => f.Target).Append(entry.Target).Where(t => !Parts.Any(p => p.Is(t)))) {
                throw new InvalidOperationException($"Rules: \"{text}\" names {Concept.Display(target)}, but the definition has no part of that concept.");
            }
            foreach (var filter in entry.Filters.Where(f => f.Kind != FilterKind.Under)) {
                RefuseNoGroup(filter.Target, filter.ToString(), text);
            }
        }
    }

    void RefuseNoGroup(Type concept, string filter, string text) {
        if (!Parts.Any(p => p.Is(concept) && p.IsGroup)) {
            throw new InvalidOperationException($"Rules: \"{text}\": {filter} needs a concept that AddGroup adds.");
        }
    }

    void RefuseNoFamily(Type concept, string text) {
        if (!Parts.Any(p => !p.IsExternal && p.InFamily(concept))) {
            throw new InvalidOperationException(
                $"Rules: \"{text}\" names {Concept.Display(concept)}, but the definition has no part of that concept in the app.");
        }
    }
}
