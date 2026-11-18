namespace Blocks.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernels;

public class BlocksApp : App<Vsa<BlocksArchitecture, VsaHost>>;
