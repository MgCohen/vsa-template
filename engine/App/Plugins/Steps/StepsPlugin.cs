namespace Steps;

using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Vsa.Composition;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernels;

public sealed class StepsPlugin : Plugin {
    protected override void Configure(IServiceCollection services) {
        services.AddScoped<StepLog>();
        services.AddScoped<RunOptions>();
        services.AddPipeline(new PipelineBuilder<IEngineStep>()
            .HandlersOf(typeof(CommandHandler<,>))
            .CallsCommandHandlers()
            .Runner<StepRunner>()
            .AfterStep<RecordStep>());
    }
}
