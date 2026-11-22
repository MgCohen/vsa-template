namespace Blocks.Host;

using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class BlocksArchitecture : VsaArchitecture {
    protected override void Define(Part root) {
        base.Define(root);
        root.AddBlock<ToolPlace>();
        root.AddBlock<LibPlace>();
    }
}
