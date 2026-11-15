namespace Notes.Features.Notebook.Slices.CreateNote;

using VSA.Contracts.Events;

public sealed record NoteCreated(Guid NoteId, Guid AuthorId, string Title) : IntegrationEvent;
