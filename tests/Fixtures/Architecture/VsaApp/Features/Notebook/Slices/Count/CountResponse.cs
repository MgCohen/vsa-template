namespace VsaApp.Features.Notebook.Slices.Count;

using Umpire.Vsa.Contracts.Requests;

public sealed record CountResponse(int Notes) : IResponse;
