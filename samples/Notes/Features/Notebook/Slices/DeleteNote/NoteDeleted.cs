namespace Notes.Features.Notebook.Slices.DeleteNote;

using Umpire.Vsa.Contracts.Events;

public sealed record NoteDeleted(Guid NoteId) : IntegrationEvent;
