using System.Text.Json;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure;

public static class ArchitectureImport
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    public static Definition Read(string json)
    {
        DefinitionDto file = JsonSerializer.Deserialize<FileDto>(json, Options)?.Definition
            ?? throw new JsonException("No definition in the file.");
        return new Definition(
            file.Name,
            [.. file.Parts.Select(p => new Part(p.Id, p.Layer, p.Parent, p.Path, p.Many, p.OfType))],
            [.. file.Rules.Select(r => new Rule(r.Id, r.Text, r.Access, TermsOf(r.From), TermsOf(r.To)))],
            [.. file.Pairs.Select(p => new Pair(p.From, p.To, p.Rule))]);
    }

    private static List<Term> TermsOf(List<TermDto> terms)
    {
        return [.. terms.Select(t => new Term(t.Layer, [.. t.Filters.Select(f => new Condition(f.Kind, f.Layer))]))];
    }

    public sealed record FileDto(DefinitionDto Definition);

    public sealed record DefinitionDto(string Name, List<PartDto> Parts, List<RuleDto> Rules, List<PairDto> Pairs);

    public sealed record PartDto(
        string Id, string Layer, string Path, bool Many, List<string> OfType, string? Parent = null);

    public sealed record RuleDto(string Id, string Text, string Access, List<TermDto> From, List<TermDto> To);

    public sealed record TermDto(string Layer, List<FilterDto> Filters);

    public sealed record FilterDto(string Kind, string Layer);

    public sealed record PairDto(string From, string To, string Rule);
}
