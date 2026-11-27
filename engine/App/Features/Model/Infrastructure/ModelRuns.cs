namespace Model.Infrastructure;

using System.Runtime.CompilerServices;
using Facts.Domain.Shared.Compile;
using Facts.Infrastructure;
using Umpire.CheckModel;

public sealed class ModelRuns(IFactSource<ModelQuery, IModelFacts> facts) {
    readonly ConditionalWeakTable<CompileProject, ModelRun> runs = new();

    public ModelRun Of(CompileProject project) => runs.GetValue(project, given => ModelRun.Start(given, facts));
}
