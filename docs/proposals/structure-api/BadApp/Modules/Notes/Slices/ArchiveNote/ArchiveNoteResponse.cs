using VSA.Contracts.Requests;

namespace BadApp.Modules.Notes.Slices.ArchiveNote;

public sealed record ArchiveNoteResponse(bool Archived) : IResponse;
