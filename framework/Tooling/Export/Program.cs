using VSA.Architecture;
using VSA.Architecture.Data;
using VSA.Architecture.Engine;

namespace Architecture.Export;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: Architecture.Export <app output folder> <app root> <architecture.json>");
            return 1;
        }
        AppFolder.Resolve(args[0]);
        ArchitectureMap architecture = ArchitectureMap.Load(typeof(ArchitectureDefinition), args[0], args[1]);
        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(architecture.AppRoot));
        string root = args[1].Replace(Path.DirectorySeparatorChar, '/');
        ArchitectureJson.Save(ArchitectureExport.Of(architecture, name, root), args[2]);
        Console.WriteLine($"{args[2]}: {architecture.Bindings.Count} bindings");
        return 0;
    }
}
