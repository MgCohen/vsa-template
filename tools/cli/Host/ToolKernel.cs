namespace Umpire.Cli.Host;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Cli.Commands;
using Umpire.Kernels;

sealed class ToolKernel : Kernel {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        foreach (var command in CommandList.All) {
            services.AddSingleton(ToolOperations.Of(command));
        }
        services.AddSingleton(ToolOperations.Help);
        services.AddSingleton(ToolOperations.Version);
    }
}
