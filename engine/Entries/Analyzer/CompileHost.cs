namespace UmpireEngine;

using Blocks;
using Build;
using Changes;
using CodeCheck;
using Facts;
using Facts.Infrastructure;
using Jobs;
using LoadApp;
using Microsoft.Extensions.DependencyInjection;
using Model;
using Model.Infrastructure;
using Operations;
using Umpire.Kernels;
using Versions;

public sealed class CompileHost : LocalHost {
    public CompileHost() {
        ModuleProvider = new FixedModules(Modules());
    }

    public static Module[] Modules() => [
        new BlocksModule(), new BuildModule(), new ChangesModule(), new CodeCheckModule(), new JobsModule(), new LoadAppModule(),
        new ModelModule(), new VersionsModule(), new FactsPlugin(), new OperationsPlugin(),
    ];

    protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => [];

    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        services.AddSingleton<IFactSource<PackQuery, bool>, PackSource>();
        services.AddSingleton<IFactSource<ModelQuery, IModelFacts>, ModelSource>();
    }
}
