using VSA.Contracts.Events;

namespace Notes.Contracts.Events;

public sealed record NoteCreated(Guid NoteId, Guid AuthorId, string Title) : IntegrationEvent;
