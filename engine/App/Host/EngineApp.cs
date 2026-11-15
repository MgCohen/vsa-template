using VSA.Composition;
using VSA.Kernel;

namespace VsaEngine;

public class EngineApp : VsaApp<EngineArchitecture> {
    protected override IAppHost Host() => new ConsoleVsaHost();
}
