namespace VsaApp.Features.Notebook.Slices.GetNote;

using Umpire.Vsa.Contracts.Requests;
using VsaApp.Features.Notebook.Slices.Shared;

public sealed record GetNoteResponse(NoteTitle Title) : IResponse;
