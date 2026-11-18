namespace NoConstructorApp.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernels;

public class NamedApp(string name) : App<Vsa<VsaHost>> {
    public string AppName => name;
}
