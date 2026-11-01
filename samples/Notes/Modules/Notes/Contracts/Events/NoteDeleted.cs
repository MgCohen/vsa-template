using VSA.Contracts.Events;

namespace Notes.Contracts.Events;

public sealed record NoteDeleted(Guid NoteId) : IntegrationEvent;
