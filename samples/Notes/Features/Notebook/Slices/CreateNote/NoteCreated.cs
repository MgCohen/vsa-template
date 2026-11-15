namespace Notes.Features.Notebook.Slices.CreateNote;

using Umpire.Vsa.Contracts.Events;

public sealed record NoteCreated(Guid NoteId, Guid AuthorId, string Title) : IntegrationEvent;
