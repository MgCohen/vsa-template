namespace Umpire.Kernels;

public static class OpenGenerics {
    public static Type? Unify(Type openImplementation, Type service) =>
        service.ContainsGenericParameters
            ? null
            : Implemented(openImplementation)
                .Select(candidate => Solve(openImplementation, candidate, service))
                .FirstOrDefault(closed => closed is not null);

    public static Type? Solve(Type openImplementation, Type pattern, Type actual) {
        var parameters = openImplementation.GetGenericArguments();
        var map = new Dictionary<Type, Type>();
        if (!Match(pattern, actual, map) || parameters.Any(p => !map.ContainsKey(p))) {
            return null;
        }
        return Close(openImplementation, [.. parameters.Select(p => map[p])]);
    }

    public static Type? Close(Type definition, IReadOnlyList<Type> arguments) {
        var parameters = definition.GetGenericArguments();
        if (parameters.Length != arguments.Count || !ConstraintTypeArguments.Meet(parameters, arguments)) {
            return null;
        }
        try {
            return definition.MakeGenericType([.. arguments]);
        } catch (ArgumentException) {
            return null;
        }
    }

    public static IEnumerable<Type> Implemented(Type type) {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType) {
            yield return current;
        }
        foreach (var contract in type.GetInterfaces()) {
            yield return contract;
        }
    }

    internal static IEnumerable<Type> CloseOver(Type definition, IReadOnlyList<Type> candidates) {
        IEnumerable<Type[]> choices = [[]];
        foreach (var parameter in definition.GetGenericArguments()) {
            var fits = ConstraintTypeArguments.For(parameter, candidates).ToList();
            choices = [.. choices.SelectMany(chosen => fits.Select(fit => (Type[])[.. chosen, fit]))];
        }
        return choices.Select(arguments => Close(definition, arguments)).OfType<Type>();
    }

    internal static string NameOf(Type definition) =>
        definition.IsGenericType
            ? definition.FullName![..definition.FullName!.IndexOf('`')].Replace('+', '.')
              + $"<{string.Join(", ", definition.GetGenericArguments().Select(a => a.Name))}>"
            : definition.FullName ?? definition.Name;

    static bool Match(Type pattern, Type actual, Dictionary<Type, Type> map) {
        if (pattern.IsGenericParameter) {
            if (map.TryGetValue(pattern, out var bound)) {
                return bound == actual;
            }
            map[pattern] = actual;
            return true;
        }
        if (!pattern.ContainsGenericParameters) {
            return pattern == actual;
        }
        if (!pattern.IsGenericType || !actual.IsConstructedGenericType
            || pattern.GetGenericTypeDefinition() != actual.GetGenericTypeDefinition()) {
            return false;
        }
        var patternArguments = pattern.GetGenericArguments();
        var actualArguments = actual.GetGenericArguments();
        for (var i = 0; i < patternArguments.Length; i++) {
            if (!Match(patternArguments[i], actualArguments[i], map)) {
                return false;
            }
        }
        return true;
    }
}
