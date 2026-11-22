namespace Umpire.Kernels;

using Umpire.Architecture;

public static class KernelParts {
    public sealed record App : Concept;

    public sealed record Kernels : Concept;

    public sealed record Operations : Concept;

    public sealed record Modules : Concept;

    public sealed record Scan : Concept;

    public sealed record Factories : Concept;

    public sealed record Services : Concept;

    public sealed record Analyzers : Concept;
}
