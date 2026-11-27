namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public static class PipelineServices {
    public static IServiceCollection AddHubStep<TStep>(this IServiceCollection services) where TStep : class, IHubStep =>
        services.AddSingleton<IHubStep, TStep>();

    public static IServiceCollection AddPipelineStep<TStep>(this IServiceCollection services) where TStep : class, IPipelineStep =>
        services.AddScoped<IPipelineStep, TStep>();

    public static IServiceCollection AddValidator<TValidator>(this IServiceCollection services) where TValidator : class, IValidator =>
        services.AddScoped<IValidator, TValidator>();
}
