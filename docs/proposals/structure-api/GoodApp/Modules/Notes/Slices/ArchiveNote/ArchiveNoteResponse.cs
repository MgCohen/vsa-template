using VSA.Contracts.Requests;

namespace GoodApp.Modules.Notes.Slices.ArchiveNote;

public sealed record ArchiveNoteResponse(bool Archived) : IResponse;
