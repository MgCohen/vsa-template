namespace VSA.Architecture;

public abstract partial class ArchitectureDefinition {
    readonly string rootPath;
    readonly List<string> conflicts = [];
    readonly List<RuleSummary> summary = [];
    Root? root;
    Options? options;
    List<PartRule>? pairs;

    protected ArchitectureDefinition(string rootPath = "") {
        this.rootPath = rootPath;
    }

    public Options Options => options ??= Configured();

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
        AddSameLayer(all);

        var book = new RuleBook(Root);
        foreach (var layer in Parts().Select(p => p.Layer!).GroupBy(l => l.GetType()).Select(g => g.First())) {
            layer.Rules(new LayerRules(layer.GetType(), book.Rules));
        }
        Rules(book);

        foreach (var rule in book.Rules) {
            AddRule(all, rule);
        }
        FindConflicts(all);
        return pairs = all;
    }

    public virtual IReadOnlyList<FolderAlias> FolderAliases(string appRoot) => [];

    protected virtual void Configure(Options options) { }

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
                case "Under":
                    if (!to.Under(filter.Target)) {
                        return null;
                    }
                    break;
                case "InSame":
                    var own = to.Groups.FirstOrDefault(i => i.Is(filter.Target));
                    if (own is null || !own.Holds(from)) {
                        return null;
                    }
                    parts.Add($"same {own.Name}");
                    break;
                case "InOther":
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
        Parts().Where(p => p.Is(entry.Target)).SelectMany(p => Expand(p, entry.Depth ?? p.Depth ?? Options.Depth)).Distinct();

    void AddSameLayer(List<PartRule> all) {
        if (!Options.UseSameLayer) {
            return;
        }
        var count = 0;
        foreach (var from in Parts()) {
            foreach (var to in Parts().Where(t => t.Layer!.GetType() == from.Layer!.GetType())) {
                all.Add(new PartRule(from, to, ApplyBoundaries(from, to, "always", null), Permission.Allow, "same layer", "same layer", to.Access ?? Options.Access));
                count++;
            }
        }
        summary.Add(new RuleSummary("same layer", "options", Permission.Allow, count));
    }

    void AddRule(List<PartRule> all, IRuleEntries rule) {
        var body = rule.Source != "definition";
        var named = rule.From.Concat(rule.Targets).SelectMany(e => e.Filters.Select(f => f.Target).Append(e.Target));
        if (body && named.Any(t => !Parts().Any(p => p.Is(t)))) {
            return;
        }
        foreach (var entry in rule.From.Concat(rule.Targets)) {
            Validate(entry, rule.Text);
        }
        var fromParts = rule.From.SelectMany(e => Branch(e).Where(p => e.Filters.All(f => p.Under(f.Target)))).Distinct().ToList();
        var count = 0;
        foreach (var entry in rule.Targets) {
            var before = count;
            foreach (var from in fromParts) {
                foreach (var to in Branch(entry)) {
                    if (Condition(from, to, entry) is not { } condition) {
                        continue;
                    }
                    if (rule.Permission == Permission.Allow) {
                        condition = ApplyBoundaries(from, to, condition, entry);
                    }
                    all.Add(new PartRule(from, to, condition, rule.Permission, rule.Source, rule.Text, entry.Access ?? to.Access ?? Options.Access));
                    count++;
                }
            }
            if (count == before && !body) {
                throw new InvalidOperationException(
                    $"Rules: \"{rule.Text}\": the target {entry} matches no part. Change the target or remove the rule.");
            }
        }
        summary.Add(new RuleSummary(rule.Text, rule.Source, rule.Permission, count));
    }

    string ApplyBoundaries(Part from, Part to, string condition, Entry? entry) {
        if (entry?.Boundary == Boundary.Open) {
            return condition;
        }
        var terms = condition == "always" ? new List<string>() : [.. condition.Split(", ")];
        foreach (var group in to.Groups.Reverse()) {
            if (entry?.Filters.Any(f => f.Kind == "InOther" && group.Is(f.Target)) == true) {
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
            throw new InvalidOperationException($"Rules: \"{text}\" names {Layer.Display(target)}, but the definition has no part of that layer.");
        }
        foreach (var filter in entry.Filters.Where(f => f.Kind != "Under" && !Parts().Any(p => p.Is(f.Target) && p.IsGroup))) {
            throw new InvalidOperationException($"Rules: \"{text}\": {filter} needs a layer that AddGroup adds.");
        }
    }

    void FindConflicts(List<PartRule> all) {
        foreach (var group in all.GroupBy(p => new { p.From, p.To })) {
            var list = group.ToList();
            for (var i = 1; i < list.Count; i++) {
                for (var j = 0; j < i; j++) {
                    if (list[j].Permission != list[i].Permission && (list[i].Condition == "always" || list[i].Condition == list[j].Condition)) {
                        conflicts.Add($"{group.Key.From.Name} ({group.Key.From.Path}) -> {group.Key.To.Name} ({group.Key.To.Path}): "
                            + $"{list[j].Permission} [{list[j].Source}] becomes {list[i].Permission} [{list[i].Source}]");
                    }
                }
            }
        }
    }
}
