namespace Notes.Features.Notebook.Slices.CreateNote;

using Umpire.Vsa.Contracts.Requests;

public sealed record CreateNoteResponse(Guid Id) : IResponse;
