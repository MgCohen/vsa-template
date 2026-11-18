namespace VsaApp.Features.Notebook.Slices.ReadNote;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record ReadNoteRequest : IRequest<ReadNoteResponse> {
    public static Effect Effect => Effect.Read;
}
