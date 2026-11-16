namespace Umpire.Kernel;

using Umpire.Architecture;

public static class PackParts {
    public sealed record Architecture : Concept;

    public sealed record Kernel : Concept;

    public sealed record Tests : Concept;
}
