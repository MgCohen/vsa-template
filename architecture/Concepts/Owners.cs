namespace Umpire.Architecture;

static class Owners {
    internal static IReadOnlyList<Type> Of(Concept concept) {
        var type = concept.GetType();
        var owners = concept.OwnedBy ?? Declared(type) ?? Nesting(type);
        foreach (var owner in owners.Where(o => !typeof(Architecture).IsAssignableFrom(o))) {
            throw new InvalidOperationException(
                $"Define: {Concept.Display(type)} names {owner.Name} as its owner, but {owner.Name} is not an Architecture. "
                + "Name an Architecture in OwnedBy or IOwnedBy<T>.");
        }
        return owners;
    }

    internal static IReadOnlyList<Type> Home(Concept concept) {
        var type = concept.GetType();
        return [.. Nesting(type).Concat(Declared(type) ?? []).Concat(concept.OwnedBy ?? []).Distinct()];
    }

    internal static bool MayPlace(IReadOnlyList<Type> owners, IReadOnlyList<Type> placers) =>
        owners.Count == 0 || owners.Any(owner => placers.Any(owner.IsAssignableFrom));

    static IReadOnlyList<Type>? Declared(Type type) {
        var owners = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOwnedBy<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToList();
        return owners.Count == 0 ? null : owners;
    }

    static IReadOnlyList<Type> Nesting(Type type) {
        for (var outer = type.DeclaringType; outer is not null; outer = outer.DeclaringType) {
            if (typeof(Architecture).IsAssignableFrom(outer)) {
                return [outer];
            }
        }
        return [];
    }
}
