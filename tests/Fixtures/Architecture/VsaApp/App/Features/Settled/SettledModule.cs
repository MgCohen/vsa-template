namespace VsaApp.Features.Settled;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class SettledModule : Module<SettledSettings> {
    protected override void Configure(IServiceCollection services, SettledSettings settings) =>
        services.AddSingleton(_ => new Part(settings.Name));

    public sealed record Part(string Name);
}
