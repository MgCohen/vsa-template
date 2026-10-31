using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.DeleteNote;

[HttpRoute(Http.Delete, "/api/notes/{id}")]
public sealed record DeleteNoteRequest(Guid Id) : Request<DeleteNoteResponse>;
