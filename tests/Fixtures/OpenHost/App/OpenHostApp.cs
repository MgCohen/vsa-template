namespace OpenHost;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public class OpenHostApp<THost> : App<VsaKernel, THost> where THost : AppHost, new();
