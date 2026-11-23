namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record Feature : Module {
    public override string GroupFolder => "Features";

    protected override void Define(Part part) {
        part.Add<RootClass>().OfType<Umpire.Kernels.Module>();
        part.Add<Domain>().Add<Domain.Shared>();
        part.Add<Infrastructure>().And<Presenters>();
        part.Add<Slice.Shared>();
        part.AddGroup<Slice>();
    }
}
