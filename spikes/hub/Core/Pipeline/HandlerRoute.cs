namespace Spike.Core;

public sealed record HandlerRoute(Type Service, IHandlerCall Call);
