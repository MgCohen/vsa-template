namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

sealed class MessageRoute {
    Type[] stepTypes = [];
    Type[] validatorTypes = [];

    public HandlerRoute Handler { get; private set; } = null!;

    public static MessageRoute For(Type message, HandlerRoute handler, IServiceProvider scope) => new() {
        Handler = handler,
        stepTypes = [.. scope.GetServices<IPipelineStep>().Where(s => s.Message.IsAssignableFrom(message)).Select(s => s.GetType())],
        validatorTypes = [.. scope.GetServices<IValidator>().Where(v => v.Message.IsAssignableFrom(message)).Select(v => v.GetType())],
    };

    public IEnumerable<IPipelineStep> Steps(IServiceProvider scope) =>
        scope.GetServices<IPipelineStep>().Where(s => stepTypes.Contains(s.GetType()));

    public IEnumerable<IValidator> Validators(IServiceProvider scope) =>
        scope.GetServices<IValidator>().Where(v => validatorTypes.Contains(v.GetType()));
}
