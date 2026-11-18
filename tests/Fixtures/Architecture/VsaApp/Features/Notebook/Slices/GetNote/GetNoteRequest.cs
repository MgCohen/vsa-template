namespace VsaApp.Features.Notebook.Slices.GetNote;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record GetNoteRequest(string Title) : IRequest<GetNoteResponse> {
    public static Effect Effect => Effect.Read;
}
