namespace Umpire.Vsa;

using Umpire.Vsa.Composition;
using Umpire.Vsa.Definition;
using Umpire.Kernels;

public class Vsa<TArchitecture, THost> : Pack<TArchitecture, VsaKernel, THost>
    where TArchitecture : VsaArchitecture, new()
    where THost : AppHost, new();

public class Vsa<THost> : Vsa<VsaArchitecture, THost> where THost : AppHost, new();
