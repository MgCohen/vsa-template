using System.Reflection;
using VSA.Architecture;

namespace Definition.Slices.LoadDefinition;

static class DefinitionFinder {
    public static ArchitectureDefinition? Find(IReadOnlyList<Assembly> app) {
        var types = app.SelectMany(TypesOf)
            .Where(t => t is { IsAbstract: false, ContainsGenericParameters: false } && t.GetConstructor(Type.EmptyTypes) is not null)
            .ToList();
        var providers = Leaves(types.Where(typeof(IArchitectureProvider).IsAssignableFrom).ToList());
        if (providers.Count > 1) {
            throw new InvalidOperationException(
                $"More than one app class gives a definition: {Names(providers)}. Keep one, or make one inherit the other.");
        }
        if (providers.Count == 1) {
            return ((IArchitectureProvider)Activator.CreateInstance(providers[0])!).Architecture;
        }
        var definitions = Leaves(types.Where(typeof(ArchitectureDefinition).IsAssignableFrom).ToList());
        return definitions.Count switch {
            0 => null,
            1 => (ArchitectureDefinition)Activator.CreateInstance(definitions[0])!,
            _ => throw new InvalidOperationException(
                $"More than one architecture definition: {Names(definitions)}. Keep one, or make one inherit the other."),
        };
    }

    static List<Type> Leaves(List<Type> types) => [.. types.Where(t => !types.Any(other => other != t && t.IsAssignableFrom(other)))];

    static string Names(IEnumerable<Type> types) => string.Join(", ", types.Select(t => t.FullName).Order(StringComparer.Ordinal));

    static IEnumerable<Type> TypesOf(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }
}
