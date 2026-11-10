using VSA.Contracts.Requests;

namespace GoodApp.Modules.Notes.Slices.CreateNote;

public sealed record CreateNoteResponse(Guid Id) : IResponse;
