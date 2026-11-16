namespace VsaApp.Features.Notebook.Slices.ReadNote;

using Umpire.Vsa.Contracts.Requests;

public sealed record ReadNoteRequest : IRequest<ReadNoteResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/read";
}
