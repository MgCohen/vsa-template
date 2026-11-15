namespace Notes.Features.Notebook.Slices.DeleteNote;

using VSA.Contracts.Events;

public sealed record NoteDeleted(Guid NoteId) : IntegrationEvent;
