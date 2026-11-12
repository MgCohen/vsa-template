using System.Text.Json;

namespace ArchitectureViewer.Infrastructure;

public static class AppImport
{
    public static FileDto Read(string json)
    {
        return JsonSerializer.Deserialize<FileDto>(json, ArchitectureImport.Options)
            ?? throw new JsonException("No app in the file.");
    }

    public sealed record FileDto(DefinitionDto Definition, AppDto App);

    public sealed record DefinitionDto(string Unlisted, List<PartDto> Parts, List<RuleDto> Rules);

    public sealed record PartDto(string Id, bool Many);

    public sealed record RuleDto(string Id, string Text);

    public sealed record AppDto(
        string Name, List<InstanceDto> Instances, List<TypeDto> Types, List<DependencyDto> Dependencies);

    public sealed record InstanceDto(string Id, string Part, string Layer, string Path, string? Parent = null);

    public sealed record TypeDto(string Id, string Name, string Instance);

    public sealed record DependencyDto(string From, string To, List<UseDto> Uses);

    public sealed record UseDto(string From, string To, string Access, string? Rule = null);
}
