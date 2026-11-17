namespace Steps;

using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Vsa.Composition;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernel;

public sealed class StepsPlugin : Plugin {
    public override void Configure(IServiceCollection services) {
        services.AddScoped<StepLog>();
        services.AddPipeline(new PipelineBuilder<IEngineStep>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StepRunner>()
            .AfterStep<RecordStep>());
    }
}
