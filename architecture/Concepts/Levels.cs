namespace Umpire.Architecture;

using System.Reflection;

static class Levels {
    const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    internal static void Call<TBase, TArgument>(TBase target, string method, TArgument argument) where TBase : class {
        foreach (var level in Chain(target.GetType(), typeof(TBase))) {
            if (level.GetMethod(method, Declared, null, [typeof(TArgument)], null) is not { IsAbstract: false } own) {
                continue;
            }
            var call = (Action<TArgument>)Activator.CreateInstance(typeof(Action<TArgument>), target, own.MethodHandle.GetFunctionPointer())!;
            call(argument);
        }
    }

    static List<Type> Chain(Type type, Type root) {
        var chain = new List<Type>();
        for (var level = type; level is not null; level = level.BaseType) {
            chain.Insert(0, level);
            if (level == root) {
                break;
            }
        }
        return chain;
    }
}
