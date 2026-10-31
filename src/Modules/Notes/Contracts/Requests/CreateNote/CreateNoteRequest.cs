using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.CreateNote;

public sealed record CreateNoteRequest(Guid AuthorId, string Title) : Request<CreateNoteResponse>, IRoutedRequest {
    public static string Method => Http.Post;
    public static string Route => "/api/notes";
}
