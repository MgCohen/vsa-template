namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Architecture.Traits;
using Umpire.Kernels.Blocks;
using Core = Umpire.Kernels.Blocks.Core;
using DefinitionRoot = Umpire.Architecture.Blocks.Definition;
using MessagesComponent = Umpire.Kernels.MessagesComponent;
using KernelParts = Umpire.Kernels.KernelParts;
using MessagesParts = Umpire.Kernels.MessagesParts;
using VsaKernel = Umpire.Vsa.Composition.VsaKernel;

public class VsaArchitecture<TKernel> : Umpire.Kernels.Architecture<TKernel> where TKernel : VsaKernel {
    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.UseSameConcept = true;
        config.Defaults.Boundary = Boundary.Closed;
        config.Roots.Add<App>("App");
        config.Roots.Add<Views>();
    }

    protected override void Define(Part root, Part kernel) {
        kernel.Add<Domain>().And<Infrastructure>().And<Presenters>();
        var contracts = kernel.Add<Contracts>();
        contracts.Add<Contracts.Requests>();
        contracts.Add<Contracts.Events>().Add<Contracts.Events.Publishing>();
        contracts.Add<Contracts.Commands>();
        kernel.Add<Handlers>().Add<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>();
        kernel.Add<Views>().And<Composition>();
        root.AddBlock<MessagesComponent>();
        root.Add<Host>();
        root.AddBlockGroup<Pack>("Packs");
        root.AddGroup<Plugin>(Boundary.Open);
        root.AddGroup<Feature>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Kernel>().Uses<Kernel>();
        rules.For<Domain>().Uses<Domain.Shared>();
        rules.For<Infrastructure>().Uses<Domain>();
        rules.For<Infrastructure>().Uses<Handlers>(Depth.Self).And<Contracts>(Depth.Self)
            .And<KernelParts.Messages>().And<MessagesParts.Contracts>();
        rules.For<Contracts>().Uses<MessagesParts.Contracts>();
        rules.For<Handlers>().Uses<KernelParts.Messages>();
        rules.For<Presenters>().Uses<Contracts>().And<MessagesParts.Contracts>()
            .And<Contracts.Requests>(Access.Internal).And<Contracts.Events>(Access.Internal).And<Domain.Shared>(Access.Internal)
            .And<Slice.Shared>(Access.Internal);
        rules.For<Presenters>().Uses<Contracts.Requests>().Under<Kernel>();
        rules.For<Presenters>().Uses<Handlers.Events>().Under<Kernel>();
        ForContractParts(rules).And<Slice.Shared>()
            .Uses<Contracts>().And<MessagesParts.Contracts>().And<Domain.Shared>().And<Slice.Shared>();
        UsesContractParts(rules.For<Slice.Shared>()).Under<Kernel>();
        UsesContractParts(rules.For<Slice.Shared>()).Under<Plugin>();
        ForHandlerParts(rules)
            .Uses<Handlers>().And<Contracts>().And<KernelParts.Messages>().And<MessagesParts.Contracts>()
            .And<Contracts.Commands>().And<Contracts.Events>().And<Slice.Shared>()
            .And<Domain>(Access.Internal).And<Infrastructure>(Access.Internal);
        rules.For<Handlers.Requests>().Uses<Contracts.Requests>();
        rules.For<Handlers.Events.Hooks>().Uses<Handlers.Events>();
        rules.For<Handlers.Commands.Hooks>().Uses<Handlers.Commands>();

        rules.For<Plugin>().Denies<Feature>();
        rules.For<Domain>().Under<Plugin>().Denies<Domain>(Depth.Self).InOther<Plugin>();

        rules.For<Feature>().Uses<Domain.Shared>().InOther<Feature>();
        rules.For<Presenters>().And<Feature>(Depth.Self).Under<Feature>().Uses<Domain.Shared>(Access.Internal).InOther<Feature>();
        ForHandlerParts(rules).Under<Feature>()
            .Uses<Contracts.Commands>(Boundary.Open).And<Contracts.Events>(Boundary.Open);

        rules.For<Module>(Depth.Self).Uses<Kernel>().And<MessagesComponent>().And<KernelParts.Messages>().And<UmpireLibraries>().And<Plugin>(Access.Internal);
        rules.For<Module>(Depth.Self).Denies<Plugin>(Depth.Self);
        rules.For<Feature>(Depth.Self).Uses<Feature>(Access.Internal);
        rules.For<Feature>(Depth.Self).Denies<Composition>();
        rules.For<Host>().Uses<Composition>().And<Core>().And<UmpireLibraries>();
        rules.For<Views>().Uses<Presenters>();
        rules.For<Views>().Uses<Hosts>().Under<Kernel>();
        rules.For<Host>().Uses<Pack>();
        rules.For<App>(Depth.Self).Uses<Host>().And<DefinitionRoot>().And<Composition>().And<Core>().And<UmpireLibraries>()
            .And<Pack>();
        rules.For<DefinitionRoot>().Uses<Core>().And<UmpireLibraries>();
        rules.For<Host>().Uses<DefinitionRoot>();
        rules.For<Host>().Uses<Module>(Depth.Self);
        rules.For<Feature>().And<Plugin>().Uses<Pack>();

        KernelRules(rules);
        BusRules(rules);

        rules.For<Handlers.Events>().Denies<Contracts.Requests>();
        rules.For<Presenters>().Denies<Contracts.Events.Publishing>().Under<Kernel>();
        rules.For<Contracts.Requests>().Under<Feature>().Are(Trait.Record | Trait.Abstract);
        rules.For<Domain.Shared>().Are<Immutable>();
    }

    static void KernelRules(RuleBook rules) {
        rules.For<Domain>().And<Infrastructure>().And<Presenters>().And<Views>().And<Composition>()
            .And<Contracts>(Depth.WithChildren).And<Handlers>(Depth.WithChildren).Under<Kernel>()
            .Denies<Domain>().And<Infrastructure>().And<Presenters>().And<Views>().And<Composition>()
            .And<Contracts>(Depth.WithChildren).And<Handlers>(Depth.WithChildren).Under<Kernel>();
        rules.For<Infrastructure>().Under<Kernel>().Uses<Domain>().Under<Kernel>();
        rules.For<Contracts.Events>().Under<Kernel>().Uses<Contracts.Events>().Under<Kernel>();
        rules.For<Handlers.Requests>().Under<Kernel>().Uses<Contracts.Requests>().Under<Kernel>();
        rules.For<Handlers.Events>().Under<Kernel>().Uses<Contracts.Events>().Under<Kernel>();
        rules.For<Handlers.Commands>().Under<Kernel>().Uses<Contracts.Commands>().Under<Kernel>();
        rules.For<Presenters>().And<Views>().And<Composition>().Under<Kernel>()
            .Uses<Contracts.Requests>().And<Contracts.Events>().And<Handlers.Events>().Under<Kernel>();
        rules.For<Views>().And<Composition>().Under<Kernel>().Uses<Presenters>().Under<Kernel>();
        rules.For<Composition>().Under<Kernel>()
            .Uses<Contracts.Commands>().And<Handlers.Requests>().And<Handlers.Commands>().Under<Kernel>();
    }

    static void BusRules(RuleBook rules) {
        rules.For<App>(Depth.Self).And<Host>().And<DefinitionRoot>().And<Kernel>(Depth.Self).Denies<KernelParts.Messages>();
        rules.For<Domain>().And<Views>().And<Composition>().And<Contracts>(Depth.WithChildren).Under<Kernel>()
            .Denies<KernelParts.Messages>();
        rules.For<KernelParts.Messages>().Denies<Kernel>(Depth.Self);
        rules.For<KernelParts.Messages>()
            .Denies<Domain>().And<Infrastructure>().And<Presenters>().And<Views>().And<Composition>()
            .And<Contracts>(Depth.WithChildren).And<Handlers>(Depth.WithChildren).Under<Kernel>();
    }

    protected static From ForContractParts(RuleBook rules) =>
        rules.For<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static Rule UsesContractParts(From from) =>
        from.Uses<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>();

    protected static From ForHandlerParts(RuleBook rules) =>
        rules.For<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>();
}
