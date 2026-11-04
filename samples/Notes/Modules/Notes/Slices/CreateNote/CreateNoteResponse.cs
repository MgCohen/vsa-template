using VSA.Contracts.Requests;

namespace Notes.Slices.CreateNote;

public sealed record CreateNoteResponse(Guid Id) : IResponse;
