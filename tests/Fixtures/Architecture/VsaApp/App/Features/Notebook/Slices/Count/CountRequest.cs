namespace VsaApp.Features.Notebook.Slices.Count;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record CountRequest : IRequest<CountResponse> {
    public static Effect Effect => Effect.Read;
}
