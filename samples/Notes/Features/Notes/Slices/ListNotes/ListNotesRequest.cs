namespace Notes.Features.Notes.Slices.ListNotes;

using VSA.Contracts.Requests;

public sealed record ListNotesRequest : IRequest<ListNotesResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/notes";
}
