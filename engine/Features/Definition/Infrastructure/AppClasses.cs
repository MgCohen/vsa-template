namespace Definition.Infrastructure;

public sealed record AppClasses(IReadOnlyList<Type> Usable, IReadOnlyList<Type> NoConstructor);
