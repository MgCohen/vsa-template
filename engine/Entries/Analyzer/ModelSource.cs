namespace UmpireEngine;

using Facts.Infrastructure;
using Model.Infrastructure;

sealed class ModelSource : IFactSource<ModelQuery, IModelFacts> {
    public IModelFacts Read(ModelQuery query) =>
        new ProjectFacts(CompileProjects.Of(query.Project), query.Block, query.Project.Model.ProjectFolder);
}
