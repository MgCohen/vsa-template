using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.DeleteNote;

public sealed record DeleteNoteRequest(Guid Id) : IRequest<DeleteNoteResponse> {
    public static string Method => Http.Delete;
    public static string Route => "/api/notes/{id}";
}
