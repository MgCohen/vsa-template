using VSA.Contracts.Requests;

namespace Notes.Slices.ListNotes;

public sealed record ListNotesRequest : IRequest<ListNotesResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/notes";
}
