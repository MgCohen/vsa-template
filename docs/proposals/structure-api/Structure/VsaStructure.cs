using Fw;

namespace Structure;

public class VsaStructure : AppStructure
{
    public readonly Part Host, Plugin, PluginRoot, PluginHandlers;
    public readonly Part Module, ModuleRoot, Domain, DomainShared, Infrastructure, Feature, Requests, Handlers;

    public VsaStructure()
    {
        Host = Add(new Part("App/Host"));

        Plugin = Add(new Part("Plugins")).Each("plugin");
        PluginRoot = Plugin.Add(new Part(typeof(PluginBase))).Exactly(1);
        PluginHandlers = Plugin.Add(new Part("Handlers"));

        Module = Add(new Part("Modules")).Each("module");
        ModuleRoot = Module.Add(new Part(typeof(ModuleBase))).Exactly(1);
        Domain = Module.Add(new Part("Domain"));
        DomainShared = Domain.Add(new Part("Shared"));
        Infrastructure = Module.Add(new Part("Infrastructure"));
        Feature = Module.Add(new Part("Slices")).Each("feature");
        Requests = Feature.Add(new Part(typeof(IRequest)));
        Handlers = Feature.Add(new Part(typeof(Handler<>))).Exactly(1);

        DomainShared.MayUse();
        Domain.MayUse(DomainShared);
        Infrastructure.MayUse(Domain, DomainShared);
        Requests.MayUse(DomainShared);
        Handlers.MayUse(Requests, Domain, DomainShared, Infrastructure);
        ModuleRoot.MayUse(Domain, DomainShared, Infrastructure, Requests, Handlers);
        PluginRoot.MayUse();
        PluginHandlers.MayUse();
        Host.MayUse(ModuleRoot, PluginRoot);

        Module.ExposeOnly(DomainShared, Requests);
        Feature.ExposeOnly();
    }
}
