using Arena.Slices.Duel;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Kernel;

namespace Arena;

public sealed class ArenaModule : Module {
    public override void Configure(IServiceCollection services) {
        services.AddSingleton<ArenaLog>();
        services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StackRunner>()
            .BeforeStep<TagStep>()
            .AfterStep<AuditStep>());
    }
}
