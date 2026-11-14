namespace VSA.Architecture.Data;

public static class DefinitionEdit {
    public static DefinitionFile Apply(DefinitionFile file, IReadOnlyList<RuleEdit> edits) {
        var texts = RuleTexts(file);
        var unknown = edits.Where(e => e.Kind != RuleEditKind.Add && !texts.Contains(e.Rule)).Select(e => e.Rule).Distinct().ToList();
        if (unknown.Count > 0) {
            throw new InvalidOperationException(
                $"The definition file has no rule {string.Join(", ", unknown)}. A rule of a concept comes from its code, "
                + "so the file cannot change it. Add a rule in the file that denies or allows the pair.");
        }
        var concepts = ConceptNames(file);
        List<RuleNode> rules = [];
        for (var i = 0; i < file.Rules.Count; i++) {
            var own = edits.Where(e => e.Kind != RuleEditKind.Add && e.Rule == texts[i]).ToList();
            rules.AddRange(own.Count == 0 ? [file.Rules[i]] : Edited(file.Rules[i], own));
        }
        rules.AddRange(edits.Where(e => e.Kind == RuleEditKind.Add).Select(e => Added(e, concepts)));
        return file with { Rules = rules };
    }

    static List<string?> RuleTexts(DefinitionFile file) {
        List<string?> texts = [.. new JsonDefinition(file).DefinitionRules().Select(r => r.Text)];
        if (texts.Count != file.Rules.Count) {
            throw new InvalidOperationException(
                $"The definition file has {file.Rules.Count} rules, and its definition gives {texts.Count}. Export the definition again.");
        }
        return texts;
    }

    static IEnumerable<RuleNode> Edited(RuleNode rule, List<RuleEdit> edits) {
        HashSet<string> removed = [.. edits.Select(e => Pair(e.From, e.To))];
        var kept = rule.From
            .Select(from => new KeptTerms(from, [.. rule.To.Where(to => !removed.Contains(Pair(Name(from), Name(to))))]))
            .Where(k => k.To.Count > 0);
        foreach (var group in kept.GroupBy(k => string.Join(",", k.To.Select(Name)))) {
            yield return new RuleNode([.. group.Select(k => k.From)], rule.Verb, group.First().To);
        }
        foreach (var edit in edits.Where(e => e.Kind == RuleEditKind.ChangeVerb)) {
            yield return new RuleNode([rule.From.First(t => Name(t) == edit.From)], edit.Verb, [rule.To.First(t => Name(t) == edit.To)]);
        }
    }

    static RuleNode Added(RuleEdit edit, Dictionary<string, string> concepts) =>
        new([TermOf(edit.From, edit.FromFilters, concepts)], edit.Verb, [TermOf(edit.To, edit.ToFilters, concepts)]);

    static TermNode TermOf(string name, IReadOnlyList<NamedFilter>? filters, Dictionary<string, string> concepts) =>
        new(TypeName(name, concepts), null, null, null,
            [.. (filters ?? []).Select(f => new FilterNode(f.Kind, TypeName(f.Concept, concepts)))]);

    static string TypeName(string name, Dictionary<string, string> concepts) =>
        concepts.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException(
                $"The definition file has no concept {name}. Use a concept of a part or a rule of the file.");

    static Dictionary<string, string> ConceptNames(DefinitionFile file) {
        IEnumerable<string> parts = file.Parts.SelectMany(Walk).Select(p => p.Concept);
        IEnumerable<string> terms = file.Rules.SelectMany(r => r.From.Concat(r.To))
            .SelectMany(t => t.Filters.Select(f => f.Concept).Append(t.Concept));
        return parts.Concat(terms).Distinct()
            .GroupBy(type => DefinitionRules.Name(JsonDefinition.TypeOf(type)))
            .ToDictionary(g => g.Key, g => g.First());
    }

    static IEnumerable<PartNode> Walk(PartNode part) => part.Children.SelectMany(Walk).Prepend(part);

    static string Name(TermNode term) => DefinitionRules.Name(JsonDefinition.TypeOf(term.Concept));

    static string Pair(string from, string to) => $"{from}>{to}";
}
