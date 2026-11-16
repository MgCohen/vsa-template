namespace VsaApp.Features.Notebook.Slices.GetNote;

using Umpire.Vsa.Contracts.Requests;

public sealed record GetNoteRequest(string Title) : IRequest<GetNoteResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/notes";
}
