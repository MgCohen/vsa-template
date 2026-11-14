using System.Text.Json.Serialization;

namespace VSA.Architecture.Data;

public sealed record DefinitionFile(
    [property: JsonPropertyName("$schema")] string Schema,
    int Version,
    string Root,
    DefinitionOptions Options,
    List<PartNode> Parts,
    List<RuleNode> Rules);
