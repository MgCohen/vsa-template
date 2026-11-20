namespace Umpire.Kernels;

using Umpire.Architecture;

public static class OpenGenerics {
    public static Type? Unify(Type openImplementation, Type service) =>
        service.ContainsGenericParameters
            ? null
            : Implemented(openImplementation)
                .Select(candidate => Solve(openImplementation, candidate, service))
                .FirstOrDefault(closed => closed is not null);

    public static Type? Solve(Type openImplementation, Type pattern, Type actual) =>
        Arguments(openImplementation, pattern, actual) is { } arguments ? Close(openImplementation, arguments) : null;

    public static Type? ForMessage(Type openImplementation, Type pattern, Type message) =>
        MessageShapeOf(pattern, message) is { } closed ? Solve(openImplementation, pattern, closed) : null;

    public static Type? MessageShapeOf(Type pattern, Type message) {
        if (pattern.IsGenericParameter) {
            return message;
        }
        if (!pattern.IsGenericType) {
            return null;
        }
        var definition = pattern.GetGenericTypeDefinition();
        return ClosedBase.OfSelfOrBase(message, definition) ?? ClosedBase.InterfaceOf(message, definition);
    }

    public static Type[]? Arguments(Type openImplementation, Type pattern, Type actual) {
        var parameters = openImplementation.GetGenericArguments();
        var map = new Dictionary<Type, Type>();
        if (!Match(pattern, actual, map) || parameters.Any(p => !map.ContainsKey(p))) {
            return null;
        }
        return [.. parameters.Select(p => map[p])];
    }

    public static string? Unmet(Type definition, IReadOnlyList<Type> arguments) =>
        ConstraintTypeArguments.Unmet(definition.GetGenericArguments(), arguments);

    public static bool UsesEachParameter(Type definition, Type pattern) {
        var used = new HashSet<Type>();
        AddParameters(pattern, used);
        return definition.GetGenericArguments().All(used.Contains);
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

    public static string NameOf(Type definition) =>
        definition.IsGenericType
            ? definition.FullName![..definition.FullName!.IndexOf('`')].Replace('+', '.')
              + $"<{string.Join(", ", definition.GetGenericArguments().Select(a => a.Name))}>"
            : definition.FullName ?? definition.Name;

    internal static IEnumerable<Type> CloseOver(Type definition, IReadOnlyList<Type> candidates) {
        IEnumerable<Type[]> choices = [[]];
        foreach (var parameter in definition.GetGenericArguments()) {
            var fits = ConstraintTypeArguments.For(parameter, candidates).ToList();
            choices = [.. choices.SelectMany(chosen => fits.Select(fit => (Type[])[.. chosen, fit]))];
        }
        return choices.Select(arguments => Close(definition, arguments)).OfType<Type>();
    }

    static void AddParameters(Type type, HashSet<Type> used) {
        if (type.IsGenericParameter) {
            used.Add(type);
        } else if (type.IsGenericType) {
            foreach (var argument in type.GetGenericArguments()) {
                AddParameters(argument, used);
            }
        } else if (type.HasElementType) {
            AddParameters(type.GetElementType()!, used);
        }
    }

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
