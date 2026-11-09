namespace VSA.Handlers;

sealed class PipelineRegistration(Type message, PipelineKind? kind, Func<IServiceProvider, IPipelineRun> build) {
    internal Type Message => message;
    internal PipelineKind? Kind => kind;
    internal Func<IServiceProvider, IPipelineRun> Build => build;
}
