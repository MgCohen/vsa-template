using VSA.Contracts.Requests;

namespace GoodApp.Modules.Notes.Slices.CreateNote;

public sealed record CreateNoteRequest(string Text, Guid? InvoiceId) : IRequest<CreateNoteResponse>
{
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/notes";
}
