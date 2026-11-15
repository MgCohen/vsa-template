using Arena.Slices.Duel;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernel;

namespace Arena;

public sealed class ArenaModule : Module {
    public override void Configure(IServiceCollection services) {
        services.AddSingleton<ArenaLog>();
        services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
            .HooksOf(typeof(BeforeCommandHook<>), typeof(AfterCommandHook<,>))
            .Runner<StackRunner>()
            .BeforeStep<TagStep>()
            .AfterStep<AuditStep>());
    }
}
