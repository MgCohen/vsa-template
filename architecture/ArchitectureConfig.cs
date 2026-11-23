namespace Umpire.Architecture;

public sealed class ArchitectureConfig {
    public PartOptions Defaults { get; } = new();
    public Roots Roots { get; } = new();
    public List<FolderAlias> FolderAliases { get; } = [];
}
