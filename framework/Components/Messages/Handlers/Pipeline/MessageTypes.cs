namespace Umpire.Messages.Handlers;

using Umpire.Architecture;

static class MessageTypes {
    internal static Type OfHandler(Type handler) =>
        handler.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>))
            ?.GetGenericArguments()[0]
        ?? throw new InvalidOperationException(
            $"{handler.FullName} is not a handler. A handler implements IHandler<TMessage>, directly or through a handler base.");

    internal static Type OfStep(Type step, Type typedStepBase) => ArgumentOf(step, typedStepBase) ?? typeof(object);

    internal static bool DerivesFrom(Type type, Type baseType) =>
        baseType.IsAssignableFrom(type) || ClosedBase.OfSelfOrBase(type, baseType) is not null;

    static Type? ArgumentOf(Type type, Type genericBase) => ClosedBase.OfSelfOrBase(type, genericBase)?.GetGenericArguments()[0];
}
