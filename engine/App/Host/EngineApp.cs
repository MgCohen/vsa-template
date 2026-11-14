using VSA.Composition;

namespace VsaEngine;

public class EngineApp : VsaApp<EngineDefinition> {
    protected override VsaBuilder Builder(string[] args) => base.Builder(args).UseConsoleHost();
}
