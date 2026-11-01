namespace Notes.Contracts.Requests.ListNotes;

public sealed record ListNotesResponse(IReadOnlyList<NoteDto> Notes);
