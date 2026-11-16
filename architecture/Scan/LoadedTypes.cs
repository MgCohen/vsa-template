namespace Umpire.Architecture;

using System.Reflection;

public static class LoadedTypes {
    public static IEnumerable<Type> Of(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }
}
