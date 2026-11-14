namespace ArchitectureViewer.Presenters.Items;

public sealed record RuleDraft(string Access, string Condition, string Layer)
{
    public const string None = "None";
    public const string Allow = "Allow";
    public const string Deny = "Deny";

    public static readonly RuleDraft Default = new(Allow, None, "Feature");

    public static readonly IReadOnlyList<string> Conditions = [None, "InSame", "InOther", "Under"];
}
