namespace Model;

using Microsoft.Extensions.DependencyInjection;
using Model.Infrastructure;
using Umpire.Kernels;

public sealed class ModelModule : Module {
    protected override void Configure(IServiceCollection services) => services.AddSingleton<ModelRuns>();
}
