namespace Notes.Features.Notebook.Slices.DeleteNote;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record DeleteNoteRequest(Guid Id) : IRequest<DeleteNoteResponse> {
    public static Effect Effect => Effect.Delete;
}
