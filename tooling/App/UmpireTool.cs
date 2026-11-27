namespace Umpire.Cli;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class UmpireTool<THost> : App<VsaKernel, THost> where THost : AppHost, new();
