using VSA.Architecture;
using VSA.Architecture.Engine;

namespace Viewer.Export;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: Viewer.Export <app output folder> <app root> <architecture.json>");
            return 1;
        }
        AppFolder.Resolve(args[0]);
        DiscoveredApp app = DiscoveredApp.FromFolder(args[0], typeof(ArchitectureDefinition), args[1]);
        AppArchitecture architecture = AppArchitecture.Load(app);
        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(app.AppRoot));
        string root = args[1].Replace(Path.DirectorySeparatorChar, '/');
        ArchitectureExport.Write(ArchitectureExport.Of(architecture, name, root), args[2]);
        Console.WriteLine($"{args[2]}: {architecture.Bindings.Count} bindings");
        return 0;
    }
}
