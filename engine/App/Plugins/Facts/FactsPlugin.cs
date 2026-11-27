namespace Facts;

using Facts.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Vsa.Composition;

public sealed class FactsPlugin : Plugin {
    protected override void Configure(IServiceCollection services) {
        services.AddSingleton<IFactSource<BinQuery, BinFacts>, BinSource>();
        services.AddSingleton<IFactSource<DefinitionQuery, AppLoader>, DefinitionSource>();
        services.AddSingleton<IFactSource<TypeFactsQuery, LoadedApp>, TypeFactSource>();
    }
}
