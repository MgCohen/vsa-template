namespace Notes.Features.Notebook.Slices.CreateNote;

using Umpire.Vsa.Contracts.Requests;

public sealed record CreateNoteRequest(Guid AuthorId, string Title) : IRequest<CreateNoteResponse>;
