namespace Umpire.Kernel;

using Umpire.Architecture;
using PackClass = Umpire.Architecture.Module;

public sealed class PackArchitecture : Architecture {
    protected override void Define(Block block) {
        block.Add<PackClass>().OfType<Pack>(required: true);
        block.Add<PackParts.Architecture>();
        block.Add<PackParts.Kernel>();
        block.Add<PackParts.Tests>();
    }
}
