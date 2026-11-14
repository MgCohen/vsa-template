namespace ArchitectureViewer.Domain.Shared;

public sealed record Condition(string Kind, string Layer)
{
    public const string Under = "Under";
    public const string InSame = "InSame";
    public const string InOther = "InOther";
}
