namespace Umpire.Kernels;

using System.Reflection;

static class ConstraintTypeArguments {
    internal static IEnumerable<Type> For(Type genericParameter, IReadOnlyList<Type> candidates) =>
        [.. candidates.Where(c => MeetsEachConstraint(genericParameter, c, bound: null))];

    internal static bool Meet(Type[] parameters, IReadOnlyList<Type> arguments) {
        var bound = new Dictionary<Type, Type>();
        for (var i = 0; i < parameters.Length; i++) {
            bound[parameters[i]] = arguments[i];
        }
        return parameters.Select((p, i) => MeetsEachConstraint(p, arguments[i], bound)).All(meets => meets);
    }

    static bool MeetsEachConstraint(Type parameter, Type candidate, Dictionary<Type, Type>? bound) {
        if (candidate.ContainsGenericParameters || candidate.IsByRef || candidate.IsPointer) {
            return false;
        }
        var flags = parameter.GenericParameterAttributes;
        if (flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) && candidate.IsValueType) {
            return false;
        }
        if (flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
            && (!candidate.IsValueType || Nullable.GetUnderlyingType(candidate) is not null)) {
            return false;
        }
        if (flags.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
            && !candidate.IsValueType
            && (candidate.IsAbstract || candidate.GetConstructor(Type.EmptyTypes) is null)) {
            return false;
        }
        if (parameter.GetGenericParameterConstraints()
            .Select(c => Substituted(c, bound))
            .Any(c => c is not null && !c.IsAssignableFrom(candidate))) {
            return false;
        }
        return !IsUnmanagedParameter(parameter) || IsUnmanaged(candidate, []);
    }

    static Type? Substituted(Type constraint, Dictionary<Type, Type>? bound) {
        if (!constraint.ContainsGenericParameters) {
            return constraint;
        }
        if (bound is null) {
            return null;
        }
        if (constraint.IsGenericParameter) {
            return bound.TryGetValue(constraint, out var argument) ? argument : null;
        }
        if (!constraint.IsGenericType) {
            return null;
        }
        var arguments = constraint.GetGenericArguments().Select(a => Substituted(a, bound)).ToArray();
        if (arguments.Any(a => a is null)) {
            return null;
        }
        try {
            return constraint.GetGenericTypeDefinition().MakeGenericType(arguments!);
        } catch (ArgumentException) {
            return null;
        }
    }

    static bool IsUnmanagedParameter(Type parameter) =>
        parameter.GetCustomAttributesData()
            .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute");

    static bool IsUnmanaged(Type type, HashSet<Type> seen) {
        if (type.IsPrimitive || type.IsEnum || type.IsPointer) {
            return true;
        }
        if (!type.IsValueType || type.IsGenericTypeDefinition) {
            return false;
        }
        if (!seen.Add(type)) {
            return true;
        }
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .All(f => IsUnmanaged(f.FieldType, seen));
    }
}
