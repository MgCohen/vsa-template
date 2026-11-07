using System.Reflection;
using System.Runtime.CompilerServices;

namespace VSA.Module;

public abstract class LayerInstaller(string layer) : IVsaInstaller {
    public virtual void Install(VsaContext context) {
        foreach (var module in context.Modules) {
            foreach (var type in ConcreteTypes(ModuleRoot.Of(module))) {
                Install(context, type);
            }
        }
    }

    protected abstract void Install(VsaContext context, Type type);

    protected static Type? ClosedBase(Type type, Type openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == openBase) {
                return b;
            }
        }
        return null;
    }

    IEnumerable<Type> ConcreteTypes(ModuleRoot root) =>
        (root.LayerAssembly(layer)?.GetTypes() ?? [])
            .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(CompilerGeneratedAttribute)));
}
