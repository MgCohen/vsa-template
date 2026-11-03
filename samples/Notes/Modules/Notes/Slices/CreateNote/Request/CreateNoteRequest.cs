using VSA.Contracts.Requests;

namespace Notes.Slices.CreateNote;

public sealed record CreateNoteRequest(Guid AuthorId, string Title) : IRequest<CreateNoteResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/notes";
}
