using Notes.Contracts.Requests.CreateNote;
using Notes.Contracts.Requests.DeleteNote;
using Notes.Contracts.Requests.ListNotes;
using VSA.Presenters;

namespace Notes.Presenters;

public sealed class NotesPresenter(IBus bus) : Presenter(bus) {
    // The sample has no sign-in, so all notes have the same author.
    static readonly Guid SampleAuthor = new("00000000-0000-0000-0000-000000000001");

    public IReadOnlyList<NoteItem> Notes { get; private set; } = [];
    public OpState Load { get; } = new();
    public OpState Create { get; } = new();
    public OpState Delete { get; } = new();

    public Task LoadAsync() => Run(Load, Refresh);

    public Task CreateAsync(string title) => Run(Create, async () => {
        await Bus.Send(new CreateNoteRequest(SampleAuthor, title));
        await Refresh();
    });

    public Task DeleteAsync(Guid id) => Run(Delete, async () => {
        await Bus.Send(new DeleteNoteRequest(id));
        await Refresh();
    });

    async Task Refresh() {
        var response = await Bus.Send(new ListNotesRequest());
        Notes = [.. response.Notes.Select(NoteItem.From)];
    }
}
