namespace Notes.Features.Notebook.Slices.CreateNote;

using VSA.Contracts.Requests;

public sealed record CreateNoteResponse(Guid Id) : IResponse;
