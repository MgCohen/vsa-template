namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared.Dll;

static class DllTypeParameters {
    const string ValueType = "System.ValueType";
    const string Unmanaged = "System.Runtime.CompilerServices.IsUnmanagedAttribute";

    public static DllTypeParameter Of(Type parameter) {
        var flags = parameter.GenericParameterAttributes;
        var isStruct = flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);
        var limits = new[] {
            flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) ? "class" : "",
            isStruct ? "struct" : "",
            ReflectionFlags.Has(parameter, Unmanaged) ? "unmanaged" : "",
            flags.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) && !isStruct ? "new()" : "",
            flags.HasFlag(GenericParameterAttributes.Contravariant) ? "in" : "",
            flags.HasFlag(GenericParameterAttributes.Covariant) ? "out" : "",
        };
        var types = ReflectionFlags.Safe(() => parameter.GetGenericParameterConstraints(), [])
            .Where(t => !(isStruct && t.FullName == ValueType));
        return new DllTypeParameter(parameter.Name, [.. limits.Where(l => l.Length > 0)], [.. types.Select(ReflectionShapes.Of)]);
    }
}
