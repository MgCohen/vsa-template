using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Architecture;
using VSA.Architecture.Vsa;
using VSA.Composition;

namespace VSA.Host;

public abstract class VsaApp<TApp> : IArchitectureProvider where TApp : VsaApp<TApp>, new() {
    public ArchitectureDefinition Architecture => new VsaDefinition(Rules);

    public static int Run(string[] args) => new TApp().Start(args);

    protected virtual int Start(string[] args) {
        using var host = Build(args);
        host.Run();
        return 0;
    }

    protected virtual IHost Build(string[] args) => Builder(args).Services(Services).Build();

    protected virtual VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).UseWebHost();

    protected virtual void Services(IServiceCollection services) { }

    protected virtual void Rules(RuleBook rules) { }
}
