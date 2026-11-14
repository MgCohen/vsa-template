namespace VSA.Architecture.Data;

public static class DefinitionExport {
    public static DefinitionFile Of(ArchitectureDefinition definition) {
        var options = definition.Options;
        return new DefinitionFile(
            DefinitionJson.SchemaName,
            DefinitionJson.Version,
            NameOf(definition),
            definition.Root.FolderName ?? "",
            new DefinitionOptions(
                options.DefaultPermission, options.Boundary, options.UseSameConcept, options.Depth, options.Access),
            [.. definition.Root.Children.Select(PartOf)],
            [.. definition.DefinitionRules().Select(RuleOf)]);
    }

    public static string NameOf(ArchitectureDefinition definition) {
        if (definition is JsonDefinition data) {
            return data.Data.Name;
        }
        var name = definition.GetType().Name;
        return name.EndsWith("Definition", StringComparison.Ordinal) && name.Length > "Definition".Length
            ? name.Substring(0, name.Length - "Definition".Length)
            : name;
    }

    public static string SourceOf(ArchitectureDefinition definition) =>
        definition is JsonDefinition data
            ? data.FilePath.Length > 0 ? Path.GetFileName(data.FilePath) : "data"
            : TypeName(definition.GetType());

    public static string TypeName(Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";

    static PartNode PartOf(Part part) => new(
        TypeName(part.Concept!.Identity), part.IsGroup, part.FolderName, part.Depth, part.Access, part.Boundary,
        [.. part.Types.Select(TypeName)], [.. part.Children.Select(PartOf)]);

    static RuleNode RuleOf(IRuleEntries rule) => new(
        [.. rule.From.Select(TermOf)],
        rule.Permission == Permission.Allow ? RuleVerb.Uses : RuleVerb.Denies,
        [.. rule.Targets.Select(TermOf)]);

    static TermNode TermOf(Entry entry) => new(
        TypeName(entry.Target), entry.Depth, entry.Access, entry.Boundary,
        [.. entry.Filters.Select(f => new FilterNode(f.Kind, TypeName(f.Target)))]);
}
