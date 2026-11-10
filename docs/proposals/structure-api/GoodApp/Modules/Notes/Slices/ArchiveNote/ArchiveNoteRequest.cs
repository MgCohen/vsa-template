using VSA.Contracts.Requests;

namespace GoodApp.Modules.Notes.Slices.ArchiveNote;

public sealed record ArchiveNoteRequest(Guid NoteId) : IRequest<ArchiveNoteResponse>
{
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/notes/{NoteId}/archive";
}
