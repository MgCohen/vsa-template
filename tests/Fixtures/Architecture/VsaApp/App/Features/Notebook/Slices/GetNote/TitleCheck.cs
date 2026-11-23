namespace VsaApp.Features.Notebook.Slices.GetNote;

using VsaApp.Features.Notebook.Slices.Shared;

abstract class TitleCheck {
    public abstract bool Passes(NoteTitle title);
}
