using System.Reflection;

namespace VSA.Testing.Core;

// The layer map: the source of truth for the architecture rules. Lower layers first, one field for each layer part.
// The map is an allow-list: if no edge permits a use, the use is forbidden.
// VSA.Sdk sets the same references in MSBuild (VSA.Layers.targets). Keep the two the same.
public static class Layers {
    // The published domain of a module: keys, ids and components that other modules read (Domain/Shared/).
    // It uses the published domain of each module that its module root lists in VsaModuleUses.
    public static readonly Layer DomainShared = new Layer("Domain.Shared")
        .Reference("Domain.Shared", Edge.ModuleOnly, listed: true);

    public static readonly Layer Domain = new Layer("Domain")
        .Reference(DomainShared, Edge.ModuleOnly);

    public static readonly Layer Infrastructure = new Layer("Infrastructure")
        .Reference(Domain)
        .Reference(DomainShared, Edge.ModuleOnly)
        .Reference("Infrastructure.EfCore", Edge.ModuleOnly);

    // Framework only: no module project has this layer.
    public static readonly Layer InfrastructureEfCore = new Layer("Infrastructure.EfCore")
        .Reference(Infrastructure);

    // The contracts use the published domain of each module: a message carries its keys and ids.
    public static readonly Layer ContractsRequests = new Layer("Contracts.Requests")
        .Reference(DomainShared, Edge.ModuleOnly, anyModule: true);

    public static readonly Layer ContractsEvents = new Layer("Contracts.Events")
        .Reference(DomainShared, Edge.ModuleOnly, anyModule: true);

    public static readonly Layer ContractsCommands = new Layer("Contracts.Commands")
        .Reference(DomainShared, Edge.ModuleOnly, anyModule: true);

    // Each handler can send the commands of all modules, and read the published domain of all modules.
    public static readonly Layer HandlersRequests = new Layer("Handlers.Requests")
        .Reference(Domain)
        .Reference(DomainShared, Edge.ModuleOnly, anyModule: true)
        .Reference(Infrastructure, Edge.ModuleOnly)
        .Reference(ContractsRequests)
        .Reference(ContractsEvents, Edge.ModuleOnly)
        .Reference(ContractsCommands, Edge.ModuleOnly, anyModule: true);

    public static readonly Layer HandlersEvents = new Layer("Handlers.Events")
        .Reference(Domain, Edge.ModuleOnly)
        .Reference(DomainShared, Edge.ModuleOnly, anyModule: true)
        .Reference(Infrastructure, Edge.ModuleOnly)
        .Reference(ContractsEvents, anyModule: true)
        .Reference(ContractsCommands, Edge.ModuleOnly, anyModule: true);

    public static readonly Layer HandlersCommands = new Layer("Handlers.Commands")
        .Reference(Domain, Edge.ModuleOnly)
        .Reference(DomainShared, Edge.ModuleOnly, anyModule: true)
        .Reference(Infrastructure, Edge.ModuleOnly)
        .Reference(ContractsEvents, Edge.ModuleOnly)
        .Reference(ContractsCommands, anyModule: true);

    // A presenter sees the events of its own module only (IPresenterEvent). A view that shows more than one module
    // shows the presenter of each module. A presenter reads the published domain of the listed modules.
    // VSA1004 stops a presenter from publishing events.
    public static readonly Layer Presenters = new Layer("Presenters")
        .Reference(DomainShared, Edge.ModuleOnly, listed: true)
        .Reference(ContractsRequests)
        .Reference(ContractsEvents, Edge.ModuleOnly);

    public static readonly Layer Views = new Layer("Views")
        .Reference(Presenters);

    // The module root: it references every layer project of its own module.
    public static readonly Layer Module = new Layer("Module")
        .Reference(DomainShared, Edge.ModuleOnly)
        .Reference(Domain, Edge.ModuleOnly)
        .Reference(Infrastructure, Edge.ModuleOnly)
        .Reference(InfrastructureEfCore, Edge.ModuleOnly)
        .Reference(ContractsRequests, Edge.ModuleOnly)
        .Reference(ContractsEvents, Edge.ModuleOnly)
        .Reference(ContractsCommands, Edge.ModuleOnly)
        .Reference(HandlersRequests, Edge.ModuleOnly)
        .Reference(HandlersEvents, Edge.ModuleOnly)
        .Reference(HandlersCommands, Edge.ModuleOnly)
        .Reference(Presenters, Edge.ModuleOnly);

    public static readonly Layer Host = new Layer("Host")
        .Reference(Module)
        .Reference(Infrastructure, Edge.BaseOnly)
        .Reference(InfrastructureEfCore, Edge.BaseOnly)
        .Reference(ContractsRequests, Edge.BaseOnly)
        .Reference(ContractsEvents, Edge.BaseOnly)
        .Reference(ContractsCommands, Edge.BaseOnly)
        .Reference(HandlersRequests, Edge.BaseOnly)
        .Reference(HandlersEvents, Edge.BaseOnly)
        .Reference(HandlersCommands, Edge.BaseOnly)
        .Reference(Presenters, Edge.BaseOnly)
        .Reference(Views, Edge.ModuleOnly);

    // All layers: reflection reads the fields once, on first use.
    public static IReadOnlyList<Layer> All => all.Value;

    public static Layer? Find(string name) => All.FirstOrDefault(l => l.Name == name);

    static readonly Lazy<IReadOnlyList<Layer>> all = new(() => [.. typeof(Layers)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(Layer))
        .Select(field => (Layer)field.GetValue(null)!)]);
}
