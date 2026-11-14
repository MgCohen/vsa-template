using VSA.Composition;
using VSA.Kernel;

namespace VsaEngine;

public class EngineApp : VsaApp<EngineDefinition> {
    protected override IAppHost Host() => new ConsoleVsaHost();
}
