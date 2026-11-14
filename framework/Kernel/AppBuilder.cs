using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace VSA.Kernel;

public sealed class AppBuilder {
    readonly string[] args;
    readonly List<Action<IServiceCollection>> services = [];
    IAppHost? host;
    IReadOnlyList<Anchor>? anchors;
    Type anchorBase = typeof(Anchor);
    IKeySource keys = new ConstraintKeys();

    AppBuilder(string[] args) {
        this.args = args;
    }

    public static AppBuilder Create(params string[] args) => new(args);

    public AppBuilder UseHost(IAppHost selected) {
        host = selected;
        return this;
    }

    public AppBuilder UseGenericHost() => UseHost(new GenericAppHost());

    public AppBuilder ScanFor(Type baseType) {
        if (!typeof(Anchor).IsAssignableFrom(baseType)) {
            throw new ArgumentException($"{baseType.FullName} does not inherit Anchor. Give an anchor base type.", nameof(baseType));
        }
        anchorBase = baseType;
        return this;
    }

    public AppBuilder Anchors(params Anchor[] given) {
        anchors = given;
        return this;
    }

    public AppBuilder Keys(IKeySource given) {
        keys = given ?? throw new ArgumentNullException(nameof(given));
        return this;
    }

    public AppBuilder Services(Action<IServiceCollection> configure) {
        services.Add(configure);
        return this;
    }

    public IAppHost SelectedHost() => host ??= new GenericAppHost();

    public IHost Build() {
        var selected = SelectedHost();
        var builder = selected.CreateBuilder(args);
        var types = Types();
        selected.Install(builder, types);
        Compose(builder.Services, builder.Configuration, types);
        return selected.Build(builder);
    }

    public IServiceCollection Compose(IServiceCollection services) {
        Compose(services, ConfigurationOf(services) ?? new ConfigurationBuilder().AddCommandLine(args).Build(), Types());
        return services;
    }

    static IConfiguration? ConfigurationOf(IServiceCollection services) =>
        services.LastOrDefault(service => service.ServiceType == typeof(IConfiguration))?.ImplementationInstance as IConfiguration;

    void Compose(IServiceCollection target, IConfiguration configuration, AnchorTypes types) {
        target.TryAddSingleton(configuration);
        target.AddLogging();
        target.AddSingleton(types);
        foreach (var installer in AnchorScan.Installers()) {
            installer.Install(target, types);
        }
        foreach (var configure in services) {
            configure(target);
        }
        foreach (var anchor in types.Anchors) {
            anchor.Configure(new AnchorBuilder(anchor.Name, target, configuration));
        }
    }

    AnchorTypes Types() => new(anchors ?? AnchorScan.Anchors(anchorBase), keys);
}
