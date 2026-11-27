namespace UmpireEngine;

using System.Runtime.CompilerServices;
using Facts.Domain.Shared.Compile;
using Microsoft.CodeAnalysis;

static class CompileProjects {
    static readonly ConditionalWeakTable<CompileProject, Compilation> Compilations = new();

    public static void Add(CompileProject project, Compilation compilation) => Compilations.Add(project, compilation);

    public static Compilation Of(CompileProject project) => Compilations.TryGetValue(project, out var compilation)
        ? compilation
        : throw new InvalidOperationException($"The compilation of {project.Name} is not known to the analyzer.");
}
