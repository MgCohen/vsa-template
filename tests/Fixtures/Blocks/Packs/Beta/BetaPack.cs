namespace Blocks.Packs.Beta;

using Umpire.Vsa;
using Umpire.Vsa.Composition;

public class Beta<THost> : Vsa<THost> where THost : VsaHost, new();
