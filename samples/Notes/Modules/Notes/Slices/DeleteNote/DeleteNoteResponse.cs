using VSA.Contracts.Requests;

namespace Notes.Slices.DeleteNote;

public sealed record DeleteNoteResponse(Guid Id) : IResponse;
