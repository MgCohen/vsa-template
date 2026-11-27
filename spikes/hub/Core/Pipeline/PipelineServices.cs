namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public static class PipelineServices {
    public static IServiceCollection AddHubStep<TStep>(this IServiceCollection services) where TStep : class, IHubStep =>
        services.AddSingleton<IHubStep, TStep>();

    public static IServiceCollection AddPipelineStep<TStep>(this IServiceCollection services) where TStep : class, IPipelineStep =>
        services.AddScoped<TStep>().AddSingleton(new StepEntry(typeof(TStep), MessageOf(typeof(TStep), typeof(PipelineStep<>))));

    public static IServiceCollection AddValidator<TValidator>(this IServiceCollection services) where TValidator : class, IValidator =>
        services.AddScoped<TValidator>().AddSingleton(new ValidatorEntry(typeof(TValidator), MessageOf(typeof(TValidator), typeof(Validator<>))));

    static Type MessageOf(Type type, Type openBase) {
        for (var current = type.BaseType; current is not null; current = current.BaseType) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openBase) {
                return current.GetGenericArguments()[0];
            }
        }
        throw new InvalidOperationException($"{type.Name} does not derive from {openBase.Name}.");
    }
}
