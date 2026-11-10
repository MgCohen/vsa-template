using System.Reflection;

namespace VSA.Module;

public sealed class ConstraintKeys : IKeySource {
    public IEnumerable<Type> KeysFor(Type genericParameter, IReadOnlyList<Type> candidates) {
        var constraints = genericParameter.GetGenericParameterConstraints();
        return [.. candidates.Where(c => MeetsEachConstraint(genericParameter, constraints, c))];
    }

    static bool MeetsEachConstraint(Type parameter, Type[] constraints, Type candidate) {
        var flags = parameter.GenericParameterAttributes;
        if (flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) && candidate.IsValueType) {
            return false;
        }
        if (flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
            && (!candidate.IsValueType || Nullable.GetUnderlyingType(candidate) is not null)) {
            return false;
        }
        if (constraints.Any(c => !c.ContainsGenericParameters && !c.IsAssignableFrom(candidate))) {
            return false;
        }
        return CanClose(parameter.DeclaringType, candidate);
    }

    static bool CanClose(Type? definition, Type candidate) {
        if (definition is null || definition.GetGenericArguments().Length != 1) {
            return true;
        }
        try {
            definition.MakeGenericType(candidate);
            return true;
        } catch (ArgumentException) {
            return false;
        }
    }
}
