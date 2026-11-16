namespace Blocks.Packs.Alpha;

using Umpire.Vsa;
using Umpire.Vsa.Composition;

public class Alpha<THost> : Vsa<THost> where THost : VsaHost, new();
