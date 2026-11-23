namespace BuiltApp.Domain.Shared;

public sealed record BlockEntry(
    string Folder, EntryForm Form, TypeName? Type, string Source, string? Problem = null, TypeName? Packed = null) {
    public string Key => Form == EntryForm.App ? nameof(EntryForm.App) : $"{Form}: {Type}";

    public BlockEntry Names(string name, string problem) => this with { Problem = $"names \"{name}\", but {problem}" };

    public override string ToString() => Form switch {
        EntryForm.App => $"the app class in {Source}",
        EntryForm.Base => $"the Block<{Type?.Name}> type in {Source}",
        EntryForm.Metadata => $"the AssemblyMetadata Umpire.Architecture of {Source}",
        _ => $"the file {Source}",
    };
}
