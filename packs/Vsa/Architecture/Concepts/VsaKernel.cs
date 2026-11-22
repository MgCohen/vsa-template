namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using KernelBlock = Umpire.Architecture.Blocks.Kernel;

public sealed record VsaKernel : KernelBlock {
    public override string Folder => "Kernel";

    protected override void Define(Part part) {
        part.Add<Domain>().And<Infrastructure>().And<Presenters>();
        part.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        part.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        part.Add<Views>().And<Composition>();
    }

    protected override void Rules(RuleBook rules) {
        AndEachLayer(AndEachLayer(rules.For<Domain>()).Uses<Domain>());
    }

    static TChain AndEachLayer<TChain>(TChain domain) where TChain : Chain<TChain> =>
        domain.And<Infrastructure>().And<Presenters>().And<Contracts>().And<Contracts.Requests>()
            .And<Contracts.Events>().And<Contracts.Commands>().And<Handlers>().And<Handlers.Requests>().And<Handlers.Events>()
            .And<Handlers.Commands>().And<Views>().And<Composition>();
}
