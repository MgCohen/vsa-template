namespace VsaApp.Host;

using Umpire.Kernels;
using VsaApp.Features.Notebook;

public sealed class FixedHost : AppHost {
    public FixedHost() {
        ModuleProvider = new FixedModules(new NotebookModule());
    }
}
