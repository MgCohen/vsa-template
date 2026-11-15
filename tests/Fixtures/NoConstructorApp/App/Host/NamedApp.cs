namespace NoConstructorApp.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

public class NamedApp(string name) : App<Vsa<VsaHost>> {
    public string AppName => name;
}
