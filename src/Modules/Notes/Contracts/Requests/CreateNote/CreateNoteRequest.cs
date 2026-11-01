using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.CreateNote;

public sealed record CreateNoteRequest(Guid AuthorId, string Title) : IRequest<CreateNoteResponse> {
    public static string Method => Http.Post;
    public static string Route => "/api/notes";
}
