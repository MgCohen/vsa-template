namespace VsaApp.Plugins.Clock.Infrastructure;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

sealed class ClockHubStep : HubStep {
    public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) =>
        Task.FromResult<Error?>(null);
}
