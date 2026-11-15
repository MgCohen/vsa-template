namespace Notes.Features.Notes.Slices.ListNotes;

using VSA.Contracts.Requests;

public sealed record ListNotesResponse(IReadOnlyList<NoteDto> Notes) : IResponse;
