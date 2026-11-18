namespace NoConstructorApp.App.Host;

using Umpire.Vsa;
using Umpire.Kernels;

public class NamedApp(string name) : App<Vsa<AppHost>> {
    public string AppName => name;
}
