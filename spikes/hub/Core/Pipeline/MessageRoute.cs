namespace Spike.Core;

sealed record MessageRoute(HandlerRoute HandlerRoute, IReadOnlyList<Type> Steps, IReadOnlyList<Type> Validators);
