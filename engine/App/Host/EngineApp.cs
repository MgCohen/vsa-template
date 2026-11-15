namespace UmpireEngine;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

public class EngineApp : App<Vsa<EngineArchitecture, ConsoleVsaHost>>;
