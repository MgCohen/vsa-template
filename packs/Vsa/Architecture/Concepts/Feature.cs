namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Kernels.Blocks;

public record Feature : Module {
    public override string GroupFolder => "Features";

    protected override void Define(Part part) {
        part.Add<Domain>().Add<Domain.Shared>();
        part.Add<Infrastructure>().And<Presenters>();
        part.Add<Slice.Shared>();
        part.AddGroup<Slice>();
    }
}
