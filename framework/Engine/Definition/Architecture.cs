namespace VSA.Architecture;

[VSA.Analyzers.Markers.RequireDerived]
public abstract class Architecture {
    const string DefinitionSource = "definition";
    readonly string rootPath;
    Root? root;
    Options? options;
    List<PartRule>? pairs;

    protected Architecture(string rootPath = "") {
        this.rootPath = rootPath;
    }

    public Options Options => options ??= Configured().Over(Options.BuiltIn);

    public Root Root {
        get {
            if (root is not null) {
                return root;
            }
            root = new Root(rootPath);
            Define(root);
            CheckShared(root);
            return root;
        }
    }

    public IEnumerable<Part> Parts() => Root.Walk().Skip(1);

    public IReadOnlyList<PartRule> Resolve() {
        if (pairs is not null) {
            return pairs;
        }
        var all = new List<PartRule>();
        AddSameConcept(all);

        var book = new RuleBook(Root);
        Rules(book);

        foreach (var rule in book.Rules) {
            AddRule(all, rule);
        }
        return pairs = all;
    }

    public virtual IReadOnlyList<FolderAlias> FolderAliases(string appRoot) => [];

    protected virtual void Configure(Options defaults) { }

    protected abstract void Define(Root root);

    protected virtual void Rules(RuleBook rules) { }

    static void CheckShared(Root root) {
        var groups = root.Walk().Skip(1).Where(p => !p.HasFolder && !p.IsGroup).GroupBy(p => p.Parent).Where(g => g.Count() > 1);
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

    static IEnumerable<Part> Expand(Part part, Depth depth) => depth switch {
        Depth.Self => [part],
        Depth.WithChildren => part.Walk(),
        _ => throw new InvalidOperationException($"Depth {depth} has no expansion. Add it to Expand."),
    };

    static string? Condition(Part from, Part to, Entry entry) {
        var parts = new List<string>();
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
                    parts.Add($"same {own.Name}");
                    break;
                case FilterKind.InOther:
                    var other = to.Groups.FirstOrDefault(i => i.Is(filter.Target));
                    if (other is null || !other.Parent!.Holds(from)) {
                        return null;
                    }
                    parts.AddRange(other.Ancestors().Where(a => a.IsGroup).Reverse().Select(a => $"same {a.Name}"));
                    parts.Add($"other {other.Name}");
                    break;
            }
        }
        return parts.Count == 0 ? "always" : string.Join(", ", parts);
    }

    Options Configured() {
        var configured = new Options();
        Configure(configured);
        return configured;
    }

    IEnumerable<Part> Branch(Entry entry) =>
        Parts().Where(p => p.Is(entry.Target)).SelectMany(p => Expand(p, entry.Depth ?? p.Depth ?? Options.Depth!.Value)).Distinct();

    void AddSameConcept(List<PartRule> all) {
        if (Options.UseSameConcept != true) {
            return;
        }
        foreach (var from in Parts()) {
            foreach (var to in Parts().Where(t => t.Concept!.GetType() == from.Concept!.GetType())) {
                all.Add(new PartRule(from, to, ApplyBoundaries(from, to, "always", null), Permission.Allow, "same concept", "same concept", to.Access ?? Options.Access!.Value));
            }
        }
    }

    void AddRule(List<PartRule> all, Rule rule) {
        var text = rule.ToString();
        foreach (var entry in rule.From.Concat(rule.Targets)) {
            Validate(entry, text);
        }
        var fromParts = rule.From.SelectMany(e => Branch(e).Where(p => e.Filters.All(f => p.Under(f.Target)))).Distinct().ToList();
        foreach (var entry in rule.Targets) {
            var matched = false;
            foreach (var from in fromParts) {
                foreach (var to in Branch(entry)) {
                    if (Condition(from, to, entry) is not { } condition) {
                        continue;
                    }
                    if (rule.Permission == Permission.Allow) {
                        condition = ApplyBoundaries(from, to, condition, entry);
                    }
                    all.Add(new PartRule(from, to, condition, rule.Permission, DefinitionSource, text, entry.Access ?? to.Access ?? Options.Access!.Value));
                    matched = true;
                }
            }
            if (!matched) {
                throw new InvalidOperationException(
                    $"Rules: \"{text}\": the target {entry} matches no part. Change the target or remove the rule.");
            }
        }
    }

    string ApplyBoundaries(Part from, Part to, string condition, Entry? entry) {
        if (entry?.Boundary == Boundary.Open) {
            return condition;
        }
        var terms = condition == "always" ? new List<string>() : [.. condition.Split(", ")];
        foreach (var group in to.Groups.Reverse()) {
            if (entry?.Filters.Any(f => f.Kind == FilterKind.InOther && group.Is(f.Target)) == true) {
                break;
            }
            if ((group.Boundary ?? Options.Boundary) == Boundary.Closed && from.Groups.Contains(group) && !terms.Contains($"same {group.Name}")) {
                terms.Add($"same {group.Name}");
            }
        }
        return terms.Count == 0 ? "always" : string.Join(", ", terms);
    }

    void Validate(Entry entry, string text) {
        foreach (var target in entry.Filters.Select(f => f.Target).Append(entry.Target).Where(t => !Parts().Any(p => p.Is(t)))) {
            throw new InvalidOperationException($"Rules: \"{text}\" names {Concept.Display(target)}, but the definition has no part of that concept.");
        }
        foreach (var filter in entry.Filters.Where(f => f.Kind != FilterKind.Under && !Parts().Any(p => p.Is(f.Target) && p.IsGroup))) {
            throw new InvalidOperationException($"Rules: \"{text}\": {filter} needs a concept that AddGroup adds.");
        }
    }
}
