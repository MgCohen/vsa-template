namespace VsaApp.Features.Notebook.Slices.GetNote;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

sealed class GetNoteValidator : Validator<GetNoteRequest> {
    public override Error? Validate(IEnvelope<GetNoteRequest> envelope) =>
        envelope.Message.Title.Length == 0 ? new Error("notes.title", "A note needs a title.") : null;
}
