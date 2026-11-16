namespace Blocks.App.Host;

using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class BlocksArchitecture : VsaArchitecture {
    protected override void Define(Block block) {
        base.Define(block);
        block.AddBlock<ToolPlace>();
        block.AddBlock<LibPlace>();
    }
}
