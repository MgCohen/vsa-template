namespace Umpire.CheckModel;

public sealed record Definition(
    int Format,
    string Name,
    IReadOnlyList<FolderAlias> FolderAliases,
    IReadOnlyList<PartDefinition> Parts,
    IReadOnlyList<UseRule> Uses,
    IReadOnlyList<CountRule> Counts,
    IReadOnlyList<MatchRule> Matches,
    IReadOnlyList<TraitRule> Traits,
    IReadOnlyList<PlaceRule> Places) {
    public const int CurrentFormat = 1;

    public string Write() => JsonWriter.Write(this);

    public static Definition Read(string text) {
        var definition = JsonReader.Read<Definition>(text);
        return definition.Format == CurrentFormat
            ? definition
            : throw new FormatException($"The definition has the format {definition.Format}. This tool reads {CurrentFormat}.");
    }
}
