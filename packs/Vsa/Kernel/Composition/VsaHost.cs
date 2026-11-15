namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Kernel;
using Umpire.Vsa.Presenters;

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
