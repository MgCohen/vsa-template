using VSA.Architecture;
using VSA.Composition;

namespace VsaEngine;

public class EngineApp : VsaApp<EngineApp> {
    public override ArchitectureDefinition Architecture => new EngineDefinition();

    protected override VsaBuilder Builder(string[] args) => base.Builder(args).UseConsoleHost();
}
