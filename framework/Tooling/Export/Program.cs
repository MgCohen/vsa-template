using System.Reflection;
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
        string bin = Path.GetFullPath(args[0]);
        string appRoot = Path.GetFullPath(args[1]);
        AppFolder.Resolve(bin);
        Assembly app = AppFolder.AppAssembly(bin);
        ArchitectureDefinition definition = AppFolder.Definition(app);
        List<Assembly> assemblies = [app, .. AppFolder.Externals(bin, definition)];
        ArchitectureMap architecture = ArchitectureMap.Load(definition, assemblies, appRoot);
        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(appRoot));
        string root = args[1].Replace(Path.DirectorySeparatorChar, '/');
        ArchitectureExport.Write(ArchitectureExport.Of(architecture, name, root), args[2]);
        Console.WriteLine($"{args[2]}: {architecture.Bindings.All.Count} bindings");
        return 0;
    }
}
