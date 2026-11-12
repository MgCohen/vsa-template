namespace VSA.Architecture;

public abstract partial class ArchitectureDefinition {
    readonly string rootPath;
    readonly List<string> conflicts = [];
    readonly List<(string Rule, string Source, Permission Permission, int Count)> summary = [];
    Root? root;
    Options? options;
    List<Pair>? pairs;

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

    public IReadOnlyList<Pair> Resolve() {
        if (pairs is not null) {
            return pairs;
        }
        var all = new List<Pair>();
        AddOwn(all);
        AddSameLayer(all);

        var map = new Map(Root);
        foreach (var layer in Parts().Select(p => p.Layer!).GroupBy(l => l.GetType()).Select(g => g.First())) {
            layer.Rules(new LayerRules(layer.GetType(), map.Rules));
        }
        Rules(map);

        foreach (var rule in map.Rules) {
            AddRule(all, rule);
        }
        FindConflicts(all);
        return pairs = all;
    }

    public virtual IReadOnlyList<FolderMap> Folders(string appRoot) => [];

    protected virtual void Configure(Options options) { }

    protected abstract void Define(Root root);

    protected virtual void Rules(Map map) { }

    static void CheckShared(Root root) {
        var groups = root.Walk().Skip(1).Where(p => !p.HasFolder && !p.IsMany).GroupBy(p => p.Parent).Where(g => g.Count() > 1);
        foreach (var shared in groups) {
            var parts = shared.ToList();
            var none = parts.FirstOrDefault(p => p.Types.Count == 0);
            if (none is not null) {
                throw new InvalidOperationException(
                    $"Define: {none.Name} shares the folder \"{shared.Key!.Path}\" with "
                    + $"{string.Join(", ", parts.Where(p => p != none).Select(p => p.Name))}, but it has no OfType. "
                    + "Add OfType<T>() to each part in a shared folder.");
            }
            var twice = parts.SelectMany(p => p.Types.Select(t => (Part: p, Type: t))).GroupBy(x => x.Type).FirstOrDefault(g => g.Count() > 1);
            if (twice is not null) {
                throw new InvalidOperationException(
                    $"Define: {string.Join(" and ", twice.Select(x => x.Part.Name))} share the folder \"{shared.Key!.Path}\" "
                    + $"and have the same OfType<{twice.Key.Name}>. Give each part a different type.");
            }
        }
    }

    static IEnumerable<Part> Expand(Part part, Reach reach) => reach switch {
        Reach.Only => [part],
        Reach.WithChildren => part.Walk(),
        _ => throw new InvalidOperationException($"Reach {reach} has no expansion. Add it to Expand."),
    };

    static string Same(IEnumerable<Part> items) =>
        string.Join(", ", items.Reverse().Select(i => $"same {i.Name}")) is { Length: > 0 } same ? same : "always";

    static string? Condition(Part from, Part to, Entry entry) {
        var parts = new List<string>();
        foreach (var filter in entry.Filters) {
            switch (filter.Kind) {
                case "Under":
                    if (!to.Under(filter.Target)) {
                        return null;
                    }
                    break;
                case "InOwn":
                    var own = to.Items.FirstOrDefault(i => i.Is(filter.Target));
                    if (own is null || !own.Holds(from)) {
                        return null;
                    }
                    parts.Add($"same {own.Name}");
                    break;
                case "InOther":
                    var other = to.Items.FirstOrDefault(i => i.Is(filter.Target));
                    if (other is null || !other.Parent!.Holds(from)) {
                        return null;
                    }
                    parts.AddRange(other.Ancestors().Where(a => a.IsMany).Reverse().Select(a => $"same {a.Name}"));
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
        Parts().Where(p => p.Is(entry.Target)).SelectMany(p => Expand(p, entry.Reach ?? p.Reach ?? Options.Reach)).Distinct();

    void AddOwn(List<Pair> all) {
        if (Options.Own == Own.None) {
            return;
        }
        var count = 0;
        foreach (var from in Parts()) {
            foreach (var to in Parts().Where(t => t.Top == from.Top)) {
                var same = Options.Own switch {
                    Own.AllGroups => to.Items.All(i => i.Holds(from)) ? to.Items.ToList() : null,
                    Own.Outer => to.Items.LastOrDefault() is { } outer ? (outer.Holds(from) ? [outer] : null) : [],
                    Own.Inner => from.Items.FirstOrDefault() is { } inner ? (inner.Holds(to) ? [inner] : null) : [],
                    _ => null,
                };
                if (same is null) {
                    continue;
                }
                all.Add(new Pair(from, to, Same(same), Permission.Allow, "own", $"own ({Options.Own})"));
                count++;
            }
        }
        summary.Add(($"own ({Options.Own})", "options", Permission.Allow, count));
    }

    void AddSameLayer(List<Pair> all) {
        if (Options.SameLayer != Permission.Allow) {
            return;
        }
        var count = 0;
        foreach (var from in Parts()) {
            foreach (var to in Parts().Where(t => t.Layer!.GetType() == from.Layer!.GetType())) {
                all.Add(new Pair(from, to, Isolate(from, to, "always", null), Permission.Allow, "same layer", "same layer", to.Access ?? Options.Access));
                count++;
            }
        }
        summary.Add(("same layer", "options", Permission.Allow, count));
    }

    void AddRule(List<Pair> all, IRuleEntries rule) {
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
                        condition = Isolate(from, to, condition, entry);
                    }
                    all.Add(new Pair(from, to, condition, rule.Permission, rule.Source, rule.Text, entry.Access ?? to.Access ?? Options.Access));
                    count++;
                }
            }
            if (count == before && !body) {
                throw new InvalidOperationException(
                    $"Rules: \"{rule.Text}\": the target {entry} matches no part. Change the target or remove the rule.");
            }
        }
        summary.Add((rule.Text, rule.Source, rule.Permission, count));
    }

    string Isolate(Part from, Part to, string condition, Entry? entry) {
        if (entry?.Isolation == Isolation.Open) {
            return condition;
        }
        var terms = condition == "always" ? new List<string>() : [.. condition.Split(", ")];
        foreach (var item in to.Items.Reverse()) {
            if (entry?.Filters.Any(f => f.Kind == "InOther" && item.Is(f.Target)) == true) {
                break;
            }
            if ((item.Isolation ?? Options.Isolation) == Isolation.Isolated && from.Items.Contains(item) && !terms.Contains($"same {item.Name}")) {
                terms.Add($"same {item.Name}");
            }
        }
        return terms.Count == 0 ? "always" : string.Join(", ", terms);
    }

    void Validate(Entry entry, string text) {
        foreach (var target in entry.Filters.Select(f => f.Target).Append(entry.Target).Where(t => !Parts().Any(p => p.Is(t)))) {
            throw new InvalidOperationException($"Rules: \"{text}\" names {Layer.Display(target)}, but the map has no part of that layer.");
        }
        foreach (var filter in entry.Filters.Where(f => f.Kind != "Under" && !Parts().Any(p => p.Is(f.Target) && p.IsMany))) {
            throw new InvalidOperationException($"Rules: \"{text}\": {filter} needs a layer that AddMany adds.");
        }
    }

    void FindConflicts(List<Pair> all) {
        foreach (var group in all.GroupBy(p => (p.From, p.To))) {
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
