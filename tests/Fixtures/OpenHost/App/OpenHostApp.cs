namespace OpenHost;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public class OpenHostApp<THost> : App<VsaKernel> where THost : AppHost;
