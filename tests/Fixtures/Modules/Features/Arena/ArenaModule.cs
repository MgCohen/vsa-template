namespace Arena;

using Arena.Slices.Duel;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernels;

public sealed class ArenaModule : Module {
    public override void Configure(IServiceCollection services) {
        services.AddSingleton<ArenaLog>();
        services.AddPipeline(new PipelineBuilder<ArenaMove>()
            .HandlersOf(typeof(CommandHandler<,>))
            .HooksOf(typeof(BeforeCommandHook<>), typeof(AfterCommandHook<,>))
            .BeforeStep<TagStep>()
            .AfterStep<AuditStep>());
    }
}
