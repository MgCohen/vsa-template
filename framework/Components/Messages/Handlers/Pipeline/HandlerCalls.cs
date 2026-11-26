namespace Umpire.Messages.Handlers;

using Umpire.Architecture;
using Umpire.Kernels;

static class HandlerCalls {
    internal static bool IsCall(Type? type) =>
        type is { IsGenericTypeDefinition: true, IsAbstract: false }
        && type.GetConstructor(Type.EmptyTypes) is not null
        && PatternOf(type) is { } pattern
        && OpenGenerics.UsesEachParameter(type, pattern);

    internal static IEnumerable<Type> Close(Type call, Type handler, Type message) {
        var pattern = PatternOf(call)!;
        return HandlerShapes(pattern.GetGenericArguments()[0], handler)
            .Select(shape => OpenGenerics.Solve(call, pattern, typeof(HandlerCall<,>).MakeGenericType(shape, message)))
            .OfType<Type>();
    }

    static Type? PatternOf(Type call) => ClosedBase.OfSelfOrBase(call, typeof(HandlerCall<,>));

    static IEnumerable<Type> HandlerShapes(Type pattern, Type handler) {
        if (pattern.IsGenericParameter) {
            return [handler];
        }
        if (!pattern.IsGenericType) {
            return pattern.IsAssignableFrom(handler) ? [pattern] : [];
        }
        var definition = pattern.GetGenericTypeDefinition();
        return ClosedBase.OfSelfOrBase(handler, definition) is { } closed
            ? [closed]
            : handler.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == definition);
    }
}
