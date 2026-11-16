namespace BuiltApp.Domain.Shared;

public sealed record BlockEntry(string Folder, EntryForm Form, string? Architecture, string Source) {
    public string Key => Form == EntryForm.App ? nameof(EntryForm.App) : $"{Form}: {Architecture}";

    public override string ToString() => Form switch {
        EntryForm.App => $"the app class in {Source}",
        EntryForm.Base => $"the Block<{Short(Architecture)}> type in {Source}",
        EntryForm.Metadata => $"the AssemblyMetadata Umpire.Architecture of {Source}",
        _ => $"the file {Source}",
    };

    static string Short(string? name) => name?.Split(',')[0].Split('.')[^1] ?? "";
}
