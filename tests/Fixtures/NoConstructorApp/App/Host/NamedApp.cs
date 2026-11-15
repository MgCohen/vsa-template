namespace NoConstructorApp.App.Host;

using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Kernel;

public class NamedApp(string name) : App<Vsa<VsaHost>> {
    public string AppName => name;
}
