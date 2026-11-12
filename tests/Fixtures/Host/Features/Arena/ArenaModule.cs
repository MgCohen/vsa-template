using Arena.Slices.Duel;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Module;

namespace Arena;

public sealed class ArenaModule : Module {
    public override void Configure(ModuleBuilder module) {
        module.Services.AddSingleton<ArenaLog>();
        module.Services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StackRunner>()
            .BeforeStep<TagStep>()
            .AfterStep<AuditStep>());
    }
}
