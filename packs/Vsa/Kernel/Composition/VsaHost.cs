namespace VSA.Composition;

using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;
using VSA.Kernel;
using VSA.Presenters;

public class VsaHost : AppHost {
    protected override void Install(IServiceCollection services, AppTypes types) {
        services.AddPipelines(types);
        services.AddRequestHandlers(types);
        services.AddEventHandlers();
        services.AddCommandHandlers(types);
        services.AddPresenters(types);
        services.AddScoped<IBus>(provider => new InProcessBus(provider));
    }
}
