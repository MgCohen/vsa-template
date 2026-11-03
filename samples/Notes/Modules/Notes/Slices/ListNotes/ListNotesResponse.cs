using VSA.Contracts.Requests;

namespace Notes.Slices.ListNotes;

public sealed record ListNotesResponse(IReadOnlyList<NoteDto> Notes) : IResponse;
