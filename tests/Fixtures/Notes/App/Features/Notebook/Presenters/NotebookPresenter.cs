namespace Notes.Features.Notebook.Presenters;

using Notes.Features.Notebook.Slices.CreateNote;
using Notes.Features.Notebook.Slices.DeleteNote;
using Notes.Features.Notebook.Slices.ListNotes;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

public sealed class NotebookPresenter(IRequestSender requests) : Presenter(requests) {
    static readonly Guid SampleAuthor = new("00000000-0000-0000-0000-000000000001");

    public IReadOnlyList<NoteItem> Notes { get; private set; } = [];
    public OpState Load { get; } = new();
    public OpState Create { get; } = new();
    public OpState Delete { get; } = new();

    public Task LoadAsync() => Run(Load, Refresh);

    public Task CreateAsync(string title) => Run(Create, async () =>
        (await Requests.SendAsync(new CreateNoteRequest(SampleAuthor, title))).Error ?? await Refresh());

    public Task DeleteAsync(Guid id) => Run(Delete, async () =>
        (await Requests.SendAsync(new DeleteNoteRequest(id))).Error ?? await Refresh());

    async Task<Error?> Refresh() {
        var listed = await Requests.SendAsync(new ListNotesRequest());
        if (listed.IsSuccess) {
            Notes = [.. listed.Value.Notes.Select(NoteItem.From)];
        }
        return listed.Error;
    }
}
