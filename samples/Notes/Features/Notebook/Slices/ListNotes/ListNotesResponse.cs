namespace Notes.Features.Notebook.Slices.ListNotes;

using VSA.Contracts.Requests;

public sealed record ListNotesResponse(IReadOnlyList<NoteDto> Notes) : IResponse;
