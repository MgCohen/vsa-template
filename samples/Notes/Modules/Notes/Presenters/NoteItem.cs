using Notes.Slices.ListNotes;

namespace Notes.Presenters;

// The view model of one note. Views see this type, not the DTO.
public sealed record NoteItem(Guid Id, string Title) {
    internal static NoteItem From(NoteDto dto) => new(dto.Id, dto.Title);
}
