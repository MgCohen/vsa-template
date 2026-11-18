namespace VsaApp.Features.Notebook.Slices.Peek;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record PeekRequest : IRequest<PeekResponse> {
    public static Effect Effect => Effect.Read;
    public static Type Handler => typeof(PeekHandler);
}
