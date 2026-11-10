using VSA.Contracts.Requests;
using VSA.Handlers.Requests;
using VSA.Module;

namespace Structure;

public class VsaStructure : AppStructure
{
    public readonly Part Host, Plugin, PluginRoot, PluginCommands;
    public readonly Part Module, ModuleRoot, Domain, DomainShared, Infrastructure;
    public readonly Part Feature, Requests, Responses, Handlers;

    public VsaStructure()
    {
        Host = Add(new Part("App/Host"));

        Plugin = Add(new Part("Plugins")).Each("plugin");
        PluginRoot = Plugin.Add(new Part(typeof(Plugin))).Exactly(1);
        PluginCommands = Plugin.Add(new Part("Contracts/Commands"));

        Module = Add(new Part("Modules")).Each("module");
        ModuleRoot = Module.Add(new Part(typeof(Module))).Exactly(1);
        Domain = Module.Add(new Part("Domain"));
        DomainShared = Domain.Add(new Part("Shared"));
        Infrastructure = Module.Add(new Part("Infrastructure"));
        Feature = Module.Add(new Part("Slices")).Each("feature");
        Requests = Feature.Add(new Part(typeof(IRequest<>)));
        Responses = Feature.Add(new Part(typeof(IResponse)));
        Handlers = Feature.Add(new Part(typeof(RequestHandler<,>))).Exactly(1);

        Host.MayUse();
        PluginRoot.MayUse();
        PluginCommands.MayUse();
        DomainShared.MayUse();
        Domain.MayUse(DomainShared);
        Infrastructure.MayUse(Domain, DomainShared);
        Responses.MayUse(DomainShared);
        Requests.MayUse(Responses, DomainShared);
        Handlers.MayUse(Requests, Responses, Domain, DomainShared, Infrastructure);
        ModuleRoot.MayUse(Domain, DomainShared, Infrastructure);

        Module.ExposeOnly(DomainShared, Requests, Responses);
        Feature.ExposeOnly();
    }
}
