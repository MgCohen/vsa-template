namespace VsaEngine;

using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Kernel;

public class EngineApp : App<Vsa<EngineArchitecture, ConsoleVsaHost>>;
