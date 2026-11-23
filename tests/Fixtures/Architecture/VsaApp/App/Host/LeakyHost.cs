namespace VsaApp.Host;

using Umpire.Kernels;
using VsaApp.Features.Notebook.Domain;

public sealed class LeakyHost : AppHost {
    public string FirstTitle() => new Note("first").Title;
}
