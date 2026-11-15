namespace Notes.Features.Notebook.Slices.DeleteNote;

using Umpire.Vsa.Contracts.Requests;

public sealed record DeleteNoteRequest(Guid Id) : IRequest<DeleteNoteResponse> {
    public static HttpMethod Method => HttpMethod.Delete;
    public static string Route => "/api/notes/{id}";
}
