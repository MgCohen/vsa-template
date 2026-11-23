namespace Notes.Features.Notebook.Slices.ListNotes;

using Umpire.Vsa.Contracts.Requests;

public sealed record ListNotesResponse(IReadOnlyList<NoteDto> Notes) : IResponse;
