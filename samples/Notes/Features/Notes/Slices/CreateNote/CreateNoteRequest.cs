namespace Notes.Features.Notes.Slices.CreateNote;

using VSA.Contracts.Requests;

public sealed record CreateNoteRequest(Guid AuthorId, string Title) : IRequest<CreateNoteResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/notes";
}
