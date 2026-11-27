namespace UmpireEngine;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public class EngineApp<THost> : App<VsaKernel, THost> where THost : AppHost, new();
