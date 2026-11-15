namespace VsaEngine;

using VSA.Composition;
using VSA.Kernel;

public class EngineApp : App<Vsa<EngineArchitecture, ConsoleVsaHost>>;
