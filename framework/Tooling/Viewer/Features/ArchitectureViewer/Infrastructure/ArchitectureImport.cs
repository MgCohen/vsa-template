using System.Text.Json;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure;

public static class ArchitectureImport
{
    private static readonly JsonSerializerOptions web = new(JsonSerializerDefaults.Web);

    public static Definition Read(string json)
    {
        DefinitionDto file = JsonSerializer.Deserialize<FileDto>(json, web)?.Definition
            ?? throw new JsonException("No definition in the file.");
        return new Definition(
            file.Name,
            [.. file.Parts.Select(p => new Part(p.Id, p.Layer, p.Parent, p.Path, p.Many, p.OfType))],
            [.. file.Rules.Select(r => RuleOf(r.Id, r.Text, r.Access))],
            [.. file.Pairs.Select(p => new Pair(p.From, p.To, p.Rule))]);
    }

    private static Rule RuleOf(string id, string text, string access)
    {
        string verb = access == Access.Deny ? " denies " : " uses ";
        string[] sides = text.Split(verb, 2);
        return new Rule(id, text, access, TermsOf(sides[0]), TermsOf(sides[1]));
    }

    private static List<Term> TermsOf(string side)
    {
        return [.. side.Split(" and ").Select(term => term.Split(' '))
            .Select(words => new Term(words[0], [.. words.Skip(1).Select(ConditionOf)]))];
    }

    private static Condition ConditionOf(string word)
    {
        int open = word.IndexOf('<', StringComparison.Ordinal);
        return new Condition(word[..open], word[(open + 1)..^1]);
    }

    public sealed record FileDto(DefinitionDto Definition);

    public sealed record DefinitionDto(string Name, List<PartDto> Parts, List<RuleDto> Rules, List<PairDto> Pairs);

    public sealed record PartDto(string Id, string Layer, string? Parent, string Path, bool Many, List<string> OfType);

    public sealed record RuleDto(string Id, string Text, string Access);

    public sealed record PairDto(string From, string To, string Rule);
}
