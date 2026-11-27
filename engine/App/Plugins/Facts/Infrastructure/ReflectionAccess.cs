namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared.Dll;

static class ReflectionAccess {
    public static DllAccess Of(Type type) {
        if (!type.IsNested) {
            return type.IsPublic ? DllAccess.Public : DllAccess.Internal;
        }
        return type.IsNestedPublic ? DllAccess.Public
            : type.IsNestedFamily ? DllAccess.Protected
            : type.IsNestedFamORAssem ? DllAccess.ProtectedInternal
            : type.IsNestedAssembly ? DllAccess.Internal
            : type.IsNestedFamANDAssem ? DllAccess.PrivateProtected
            : DllAccess.Private;
    }

    public static DllAccess Of(MethodBase method) =>
        method.IsPublic ? DllAccess.Public
        : method.IsFamily ? DllAccess.Protected
        : method.IsFamilyOrAssembly ? DllAccess.ProtectedInternal
        : method.IsAssembly ? DllAccess.Internal
        : method.IsFamilyAndAssembly ? DllAccess.PrivateProtected
        : DllAccess.Private;

    public static DllAccess Of(FieldInfo field) =>
        field.IsPublic ? DllAccess.Public
        : field.IsFamily ? DllAccess.Protected
        : field.IsFamilyOrAssembly ? DllAccess.ProtectedInternal
        : field.IsAssembly ? DllAccess.Internal
        : field.IsFamilyAndAssembly ? DllAccess.PrivateProtected
        : DllAccess.Private;

    public static DllAccess Widest(IEnumerable<MethodInfo?> accessors) =>
        accessors.OfType<MethodInfo>().Select(Of).DefaultIfEmpty(DllAccess.Private).Max();
}
