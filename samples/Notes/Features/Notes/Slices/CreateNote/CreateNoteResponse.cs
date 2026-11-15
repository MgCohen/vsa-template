namespace Notes.Features.Notes.Slices.CreateNote;

using VSA.Contracts.Requests;

public sealed record CreateNoteResponse(Guid Id) : IResponse;
