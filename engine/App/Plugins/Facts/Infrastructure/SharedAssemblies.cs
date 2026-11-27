namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared;
using Umpire.Architecture;

public static class SharedAssemblies {
    static readonly HashSet<string> Names = new(
        Closure(typeof(Architecture).Assembly).Concat(Closure(typeof(Umpire.Kernels.Module).Assembly)),
        StringComparer.OrdinalIgnoreCase);

    public static bool Contains(string name) => Names.Contains(name);

    static HashSet<string> Closure(Assembly root) {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.GetName().Name! };
        foreach (var reference in root.GetReferencedAssemblies().Where(r => !BinDll.IsSystem(r.Name!))) {
            names.UnionWith(Closure(Assembly.Load(reference)));
        }
        return names;
    }
}
