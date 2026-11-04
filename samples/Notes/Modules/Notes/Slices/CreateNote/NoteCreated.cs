using VSA.Contracts.Events;

namespace Notes.Slices.CreateNote;

public sealed record NoteCreated(Guid NoteId, Guid AuthorId, string Title) : IntegrationEvent;
