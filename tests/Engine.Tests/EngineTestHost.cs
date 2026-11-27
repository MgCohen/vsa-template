namespace Engine.Tests;

using Facts.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using UmpireEngine;

sealed class EngineTestHost : AppHost {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        services.AddSingleton<IFactSource<DefinitionQuery, IDefinitionLoader>, DefinitionSource>();
    }
}
