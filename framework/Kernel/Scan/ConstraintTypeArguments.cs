namespace Umpire.Kernels;

using System.Reflection;

static class ConstraintTypeArguments {
    internal static IEnumerable<Type> For(Type genericParameter, IReadOnlyList<Type> candidates) =>
        [.. candidates.Where(c => MeetsEachConstraint(genericParameter, c, bound: null))];

    internal static bool Meet(Type[] parameters, IReadOnlyList<Type> arguments) =>
        Unmet(parameters, arguments) is null;

    internal static string? Unmet(Type[] parameters, IReadOnlyList<Type> arguments) {
        var bound = new Dictionary<Type, Type>();
        for (var i = 0; i < parameters.Length; i++) {
            bound[parameters[i]] = arguments[i];
        }
        for (var i = 0; i < parameters.Length; i++) {
            if (!MeetsEachConstraint(parameters[i], arguments[i], bound)) {
                return $"{arguments[i].Name} does not meet the constraints of {parameters[i].Name}: "
                    + ConstraintsOf(parameters[i]);
            }
        }
        return null;
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

    static string ConstraintsOf(Type parameter) {
        var flags = parameter.GenericParameterAttributes;
        var unmanaged = IsUnmanagedParameter(parameter);
        var names = new List<string>();
        if (unmanaged) {
            names.Add("unmanaged");
        } else if (flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) {
            names.Add("struct");
        }
        if (flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) {
            names.Add("class");
        }
        names.AddRange(parameter.GetGenericParameterConstraints()
            .Where(c => c != typeof(ValueType))
            .Select(c => c.Name));
        if (flags.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
            && !flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) {
            names.Add("new()");
        }
        return string.Join(", ", names);
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
