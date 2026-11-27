namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Presenters;

public class VsaKernel : Kernel {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        services.AddRequestHandlers(types);
        services.AddEventHandlers();
        services.AddCommandHandlers(types);
        services.AddPresenters(types);
        services.AddRequestConverter(types);
    }
}
