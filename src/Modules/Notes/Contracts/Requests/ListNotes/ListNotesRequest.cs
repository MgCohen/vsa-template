using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.ListNotes;

public sealed record ListNotesRequest : IRequest<ListNotesResponse> {
    public static string Method => Http.Get;
    public static string Route => "/api/notes";
}
