namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Kernels.Blocks;

public record Plugin : Module {
    public override string GroupFolder => "Plugins";

    protected override void Rules(RuleBook rules) => rules.For<Plugin>(Depth.Self).HasTypes<Umpire.Vsa.Composition.Plugin>().AtLeast(1);

    protected override void Define(Part part) {
        part.Add<Domain>().Add<Domain.Shared>();
        part.Add<Infrastructure>().And<Presenters>();
        part.Add<Contracts>().Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();
        var handlers = part.Add<Handlers>();
        handlers.Add<Handlers.Requests>();
        handlers.Add<Handlers.Events>().Add<Handlers.Events.Hooks>().InParentFolder();
        handlers.Add<Handlers.Commands>().Add<Handlers.Commands.Hooks>().InParentFolder();
    }
}
