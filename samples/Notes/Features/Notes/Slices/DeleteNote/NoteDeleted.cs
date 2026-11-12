using VSA.Contracts.Events;

namespace Notes.Slices.DeleteNote;

public sealed record NoteDeleted(Guid NoteId) : IntegrationEvent;
