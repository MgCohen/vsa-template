namespace VsaApp.Features.Notebook.Slices.GetNote;

using VsaApp.Features.Notebook.Slices.Shared;

sealed class ShortTitleCheck : TitleCheck {
    public override bool Passes(NoteTitle title) => title.Value.Length <= 80;
}
