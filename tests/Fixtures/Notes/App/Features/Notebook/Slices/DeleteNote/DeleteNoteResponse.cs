namespace Notes.Features.Notebook.Slices.DeleteNote;

using Umpire.Vsa.Contracts.Requests;

public sealed record DeleteNoteResponse(Guid Id) : IResponse;
