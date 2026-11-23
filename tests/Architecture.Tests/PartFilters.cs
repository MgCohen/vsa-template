namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

static class PartFilters {
    public static IEnumerable<Part> WithoutRoots(this IEnumerable<Part> parts) => parts.Where(p => p.Concept is not (App or Definition));
}
