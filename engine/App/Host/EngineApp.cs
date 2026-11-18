namespace UmpireEngine;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernels;

public class EngineApp : App<Vsa<EngineArchitecture, ConsoleVsaHost>>;
