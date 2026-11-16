namespace Notes.Features.Notebook.Slices.CreateNote;

using Notes.Features.Notebook.Domain;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Infrastructure;

sealed class CreateNoteHandler(IRepository<Note, NoteId> notes, IIntegrationEventPublisher events)
    : RequestHandler<CreateNoteRequest, CreateNoteResponse> {
    static readonly Error TitleRequired = new("notes.title_required", "A note must have a title.");
    static readonly Error TitleTooLong =
        new("notes.title_too_long", $"A title must have {Note.MaxTitleLength} characters or fewer.");

    public override async Task<Result<CreateNoteResponse>> HandleAsync(Envelope<CreateNoteRequest> envelope) {
        var title = envelope.Message.Title;
        if (!Note.IsValidTitle(title)) {
            return string.IsNullOrWhiteSpace(title) ? TitleRequired : TitleTooLong;
        }
        var note = Note.Create(new AuthorId(envelope.Message.AuthorId), title);
        notes.Add(note);
        await notes.SaveChangesAsync(envelope.Cancellation);
        await events.PublishAsync(new NoteCreated(note.Id.Value, note.Author.Value, note.Title), envelope.Cancellation);
        return new CreateNoteResponse(note.Id.Value);
    }
}
