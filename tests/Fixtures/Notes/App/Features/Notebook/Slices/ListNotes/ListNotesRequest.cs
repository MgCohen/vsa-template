namespace Notes.Features.Notebook.Slices.ListNotes;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record ListNotesRequest : IRequest<ListNotesResponse> {
    public static Effect Effect => Effect.Read;
}
