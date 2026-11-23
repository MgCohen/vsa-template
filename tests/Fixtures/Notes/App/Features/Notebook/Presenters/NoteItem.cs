namespace Notes.Features.Notebook.Presenters;

using Notes.Features.Notebook.Slices.ListNotes;

public sealed record NoteItem(Guid Id, string Title) {
    internal static NoteItem From(NoteDto dto) => new(dto.Id, dto.Title);
}
