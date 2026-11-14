using VSA.Composition;

namespace VsaEngine;

public class EngineApp : VsaApp<EngineDefinition> {
    protected override IVsaHost Host() => new ConsoleVsaHost();
}
