namespace Umpire.CheckModel.Export;

using Api = Umpire.Architecture;

public sealed class DefinitionExport {
    readonly Api.ResolvedArchitecture resolved;
    readonly Dictionary<Api.Part, int> positions;

    DefinitionExport(Api.ResolvedArchitecture resolved) {
        this.resolved = resolved;
        positions = resolved.Root.Walk().Skip(1).Select((part, i) => KeyValuePair.Create(part, i)).ToDictionary();
    }

    public static Definition Of(Api.ResolvedArchitecture resolved) => new DefinitionExport(resolved).Make();

    static TypeName Name(Type type) => new(type.FullName!, type.Assembly.GetName().Name!);

    static TypeName? NameOrNull(Type? type) => type is null ? null : Name(type);

    static TOut Same<TOut>(Enum value) where TOut : struct, Enum => Enum.Parse<TOut>(value.ToString());

    static TOut? SameOrNull<TOut>(Enum? value) where TOut : struct, Enum => value is null ? null : Same<TOut>(value);

    static FolderAlias Alias(Api.FolderAlias alias) => new(alias.Folder, alias.As);

    static List<RuleEntry> Entries(IEnumerable<Api.Entry> entries) => [.. entries.Select(entry => new RuleEntry(
        Name(entry.Target),
        [.. entry.Filters.Select(filter => new RuleFilter(Same<FilterKind>(filter.Kind), Name(filter.Target)))],
        SameOrNull<Access>(entry.Access), SameOrNull<Depth>(entry.Depth), SameOrNull<Boundary>(entry.Boundary)))];

    Definition Make() {
        var compiled = resolved.Compiled;
        return new Definition(
            Definition.CurrentFormat,
            resolved.Architecture.GetType().FullName!,
            [.. resolved.Config.FolderAliases.Select(Alias)],
            [.. resolved.Root.Children.Select(Part)],
            Uses(compiled.Book.Rules),
            [.. compiled.Counts.Select((statement, i) => Count(statement, compiled.Book.Counts.ElementAtOrDefault(i)))],
            [.. compiled.Matches.Zip(compiled.Book.Matches, Match)],
            [.. compiled.Traits.Zip(compiled.Book.Traits, Trait)],
            [.. compiled.Places.Zip(compiled.Book.Places, Place)]);
    }

    IReadOnlyList<int> Positions(IEnumerable<Api.Part> parts) => [.. parts.Select(part => positions[part])];

    IReadOnlyList<int> PositionsWhere(Func<Api.Part, bool> fits) => Positions(resolved.Parts.Where(fits));

    PartDefinition Part(Api.Part part) => new(
        Name(part.Concept!.GetType()),
        part.Concept.Folder,
        part.IsGroup,
        part.IsBlockPlace,
        part.IsKernelRoot,
        part.Concept is Api.Blocks.Block,
        part.Concept is Api.Blocks.App,
        Options(resolved.OptionsOf(part)),
        [.. part.Types.Select(Name)],
        [.. part.Children.Select(Part)],
        part.Folder,
        part.Assembly,
        part.NamePattern is { } name ? NameRule(part, name) : null);

    static PartOptions Options(Api.PartOptions options) => new(
        Same<Permission>(options.DefaultPermission!.Value),
        Same<Boundary>(options.Boundary!.Value),
        options.UseSameConcept!.Value,
        Same<Depth>(options.Depth!.Value),
        Same<Access>(options.Access!.Value),
        options.Required!.Value);

    NameRule NameRule(Api.Part part, Api.NamePattern name) => new(
        name.Start, name.End, name.OfAGroup, NameOrNull(name.Group), name.GroupOf(part) is { } group ? positions[group] : null);

    List<UseRule> Uses(IReadOnlyList<Api.Rule> rules) {
        var uses = new List<UseRule>();
        var start = 0;
        while (start < resolved.UseRules.Count) {
            var first = resolved.UseRules[start];
            var pairs = resolved.UseRules.Skip(start).TakeWhile(rule => rule.Text == first.Text).ToList();
            var rule = rules.FirstOrDefault(r => r.ToString() == first.Text);
            uses.Add(new UseRule(
                rule is null, Entries(rule?.From ?? []), Same<Permission>(first.Permission), Entries(rule?.Targets ?? []),
                [.. pairs.Select(Pair)]));
            start += pairs.Count;
        }
        return uses;
    }

    UsePair Pair(Api.PartRule rule) => new(
        positions[rule.From], positions[rule.To], Same<Access>(rule.Access),
        Positions(rule.Condition.Same), Positions(rule.Condition.Other));

    CountRule Count(Api.CountStatement statement, Api.Count? count) => new(
        Entries(count?.Subjects ?? []),
        Positions(statement.Subjects),
        statement.WholeApp,
        statement.PerPart,
        statement.SkipTaken,
        count is null,
        new CountedTypes(
            statement.CountedParts is null, Positions(statement.CountedParts ?? []),
            count is { Types: false } ? Name(count.Counted) : null, SameOrNull<Depth>(count?.Depth), NameOrNull(statement.CountedType)),
        statement.OneKind,
        statement.Min,
        statement.Max);

    MatchRule Match(Api.MatchStatement statement, Api.Match match) => new(
        Entries(match.Subjects), Positions(statement.Subjects), Name(statement.Taken),
        PositionsWhere(p => p.InFamily(statement.Taken)),
        statement.Group is { } group ? PositionsWhere(p => p.Is(group)) : [],
        NameOrNull(statement.Group));

    TraitRule Trait(Api.TraitStatement statement, Api.TraitRule rule) =>
        new(Entries(rule.Subjects), Positions(statement.Subjects), TraitExport.Of(statement.Condition));

    PlaceRule Place(Api.PlaceStatement statement, Api.PlaceLimit limit) => new(
        new TypeName(Api.TypeText.OpenName(statement.Type), statement.Type.Assembly.GetName().Name!),
        typeof(Attribute).IsAssignableFrom(statement.Type),
        Entries(limit.Places),
        Positions(statement.Places));
}
