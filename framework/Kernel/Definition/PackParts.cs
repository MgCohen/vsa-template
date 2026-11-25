namespace Umpire.Kernels;

using Umpire.Architecture;

public static class PackParts {
    public sealed record Kernel : Umpire.Architecture.Blocks.Kernel;

    public sealed record Tests : Concept;
}
