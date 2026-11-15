namespace Notes.Features.Notebook.Slices.ListNotes;

using Umpire.Vsa.Contracts.Requests;

public sealed record ListNotesRequest : IRequest<ListNotesResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/notes";
}
