using Engine.Slices.RunEngine;
using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Module;

namespace Engine;

public sealed class EngineModule : Module {
    public override void Configure(ModuleBuilder module) {
        module.Services.AddScoped<StepLog>();
        module.Services.AddPipeline(new PipelineBuilder<IEngineStep>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StepRunner>()
            .AfterStep<RecordStep>());
    }
}
