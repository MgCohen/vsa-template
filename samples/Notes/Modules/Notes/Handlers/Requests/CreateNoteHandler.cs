using Notes.Contracts.Events;
using Notes.Contracts.Requests.CreateNote;
using Notes.Domain;
using VSA.Contracts.Events;
using VSA.Domain;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace Notes.Handlers.Requests;

sealed class CreateNoteHandler(IRepository<Note, NoteId> notes, IIntegrationEventPublisher events)
    : RoutedHandler<CreateNoteRequest, CreateNoteResponse> {
    protected override async Task<Result<CreateNoteResponse>> HandleRequestAsync(CreateNoteRequest request, CancellationToken ct) {
        var created = Note.Create(new AuthorId(request.AuthorId), request.Title);
        if (!created.IsSuccess) {
            return created.Error!;
        }
        var note = created.Value;
        notes.Add(note);
        await notes.SaveChangesAsync(ct);
        await events.PublishAsync(new NoteCreated(note.Id.Value, note.Author.Value, note.Title), ct);
        return new CreateNoteResponse(note.Id.Value);
    }
}
