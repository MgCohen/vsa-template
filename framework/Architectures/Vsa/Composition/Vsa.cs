namespace VSA.Composition;

using VSA.Architecture.Vsa;
using VSA.Kernel;

public class Vsa<TArchitecture, THost> : Pack<TArchitecture, THost>
    where TArchitecture : VsaArchitecture, new()
    where THost : VsaHost, new();

public class Vsa<THost> : Vsa<VsaArchitecture, THost> where THost : VsaHost, new();
