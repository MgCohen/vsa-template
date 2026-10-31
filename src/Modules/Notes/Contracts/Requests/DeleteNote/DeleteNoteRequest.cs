using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.DeleteNote;

public sealed record DeleteNoteRequest(Guid Id) : Request<DeleteNoteResponse>, IRoutedRequest {
    public static string Method => Http.Delete;
    public static string Route => "/api/notes/{id}";
}
