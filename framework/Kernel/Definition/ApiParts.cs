namespace Umpire.Kernels;

using Umpire.Architecture;

static class ApiParts {
    public sealed record Concepts : Concept;

    public sealed record Markers : Concept;

    public sealed record Options : Concept;

    public sealed record Parts : Concept;

    public sealed record Rules : Concept;
}
