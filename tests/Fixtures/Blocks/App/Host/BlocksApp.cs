namespace Blocks.App.Host;

using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

public class BlocksApp : App<Vsa<BlocksArchitecture, VsaHost>>;
