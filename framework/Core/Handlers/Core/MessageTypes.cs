namespace VSA.Handlers;

static class MessageTypes {
    internal static Type OfHandler(Type handler) =>
        handler.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>))
            ?.GetGenericArguments()[0]
        ?? throw new InvalidOperationException(
            $"{handler.FullName} is not a handler. A handler implements IHandler<TMessage>, "
            + "for example through CommandHandler<,>, RequestHandler<,> or IntegrationEventHandler<>.");

    internal static Type OfStep(Type step, Type stepBase) =>
        OfBase(step, stepBase) ?? throw new InvalidOperationException(
            $"{step.FullName} is not a {stepBase.Name.Split('`')[0]}<TMessage>. Derive it from that class.");

    internal static Type? OfBase(Type type, Type genericBase) {
        for (var b = type; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == genericBase) {
                return b.GetGenericArguments()[0];
            }
        }
        return null;
    }
}
