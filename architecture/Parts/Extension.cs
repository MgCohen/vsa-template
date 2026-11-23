namespace Umpire.Architecture;

using Umpire.Architecture.Blocks;

internal sealed record Extension(Block Body, Type Target, IReadOnlyList<Type> Under);
