namespace Umpire.Messages.Handlers;

static class MessageTypes {
    internal static Type OfHandler(Type handler) =>
        handler.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>))
            ?.GetGenericArguments()[0]
        ?? throw new InvalidOperationException(
            $"{handler.FullName} is not a handler. A handler implements IHandler<TMessage>, directly or through a handler base.");

    internal static Type OfStep(Type step, Type typedStepBase) => OfBase(step, typedStepBase) ?? typeof(object);

    internal static bool DerivesFrom(Type type, Type baseType) =>
        baseType.IsAssignableFrom(type) || OfBase(type, baseType) is not null;

    internal static Type? OfBase(Type type, Type genericBase) {
        for (var b = type; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == genericBase) {
                return b.GetGenericArguments()[0];
            }
        }
        return null;
    }
}
