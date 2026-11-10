using VSA.Contracts.Requests;

namespace BadApp.Modules.Notes.Slices.CreateNote;

public sealed record CreateNoteResponse(Guid Id) : IResponse;
