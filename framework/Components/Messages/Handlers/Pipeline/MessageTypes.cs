namespace Umpire.Messages.Handlers;

using Umpire.Architecture;
using Umpire.Kernels;

static class MessageTypes {
    internal static IReadOnlyList<Type> OfHandler(Type handler) {
        Type[] messages = [.. handler.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>))
            .Select(i => i.GetGenericArguments()[0])];
        return messages.Length > 0
            ? messages
            : throw new InvalidOperationException(
                $"{handler.FullName} is not a handler. A handler implements IHandler<TMessage>, directly or through a handler base.");
    }

    internal static Type OfStep(Type step, Type typedStepBase) => ArgumentOf(step, typedStepBase) ?? typeof(object);

    internal static bool DerivesFrom(Type type, Type baseType) =>
        baseType.IsAssignableFrom(type) || ClosedBase.OfSelfOrBase(type, baseType) is not null;

    internal static bool Applies(Type part, Type message) => part.IsAssignableFrom(message);

    internal static bool IsNarrower(Type message, Type than) => message != than && than.IsAssignableFrom(message);

    internal static List<T> Narrowest<T>(IReadOnlyList<T> items, Func<T, Type> messageOf) =>
        [.. items.Where(item => !items.Any(other => IsNarrower(messageOf(other), messageOf(item))))];

    internal static List<T> NearestFirst<T>(IEnumerable<T> items, Func<T, Type> messageOf) {
        var list = items.ToList();
        return [.. list
            .OrderByDescending(item => list.Count(other => IsNarrower(messageOf(item), messageOf(other))))
            .ThenBy(item => messageOf(item).FullName, StringComparer.Ordinal)];
    }

    internal static IEnumerable<T> NearestPerDefinition<T>(
        IEnumerable<T> nearestFirst, Func<T, Type> typeOf, Func<T, Type> messageOf) =>
        nearestFirst
            .GroupBy(item => DefinitionOf(typeOf(item)))
            .SelectMany(group => Narrowest(group.ToList(), messageOf))
            .GroupBy(typeOf)
            .Select(group => group.First());

    internal static Type? Closed(Type open, Type pattern, Type message) =>
        OpenGenerics.ForMessage(open, pattern, message);

    internal static string? CloseFailure(Type open, Type pattern, Type message) =>
        OpenGenerics.MessageShapeOf(pattern, message) is { } shape
        && OpenGenerics.Arguments(open, pattern, shape) is { } arguments
        && OpenGenerics.Unmet(open, arguments) is { } unmet
            ? $"{OpenGenerics.NameOf(open)} cannot close for {message.Name}: {unmet}."
            : null;

    static Type DefinitionOf(Type type) => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

    static Type? ArgumentOf(Type type, Type genericBase) => ClosedBase.OfSelfOrBase(type, genericBase)?.GetGenericArguments()[0];
}
