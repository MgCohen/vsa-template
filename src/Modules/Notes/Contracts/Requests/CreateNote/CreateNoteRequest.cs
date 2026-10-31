using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.CreateNote;

[HttpRoute(Http.Post, "/api/notes")]
public sealed record CreateNoteRequest(Guid AuthorId, string Title) : Request<CreateNoteResponse>;
