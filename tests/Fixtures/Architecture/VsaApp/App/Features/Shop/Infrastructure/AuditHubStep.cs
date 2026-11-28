namespace VsaApp.Features.Shop.Infrastructure;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

sealed class AuditHubStep : HubStep {
    public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) =>
        Task.FromResult<Error?>(null);
}
