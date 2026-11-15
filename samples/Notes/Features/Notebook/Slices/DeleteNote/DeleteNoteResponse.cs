namespace Notes.Features.Notebook.Slices.DeleteNote;

using VSA.Contracts.Requests;

public sealed record DeleteNoteResponse(Guid Id) : IResponse;
