namespace Umpire.Cli.Plugins.Commands.Handlers.Requests;

using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

public abstract class ToolHandler<TRequest, TResponse> : RequestHandler<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : ToolResponse {
    public sealed override async Task<Result<TResponse>> HandleAsync(IEnvelope<TRequest> envelope) {
        try {
            return await Run(envelope.Message, envelope.Cancellation).ConfigureAwait(false);
        } catch (UmpireException e) {
            return new Error(UmpireException.Code, e.Message);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return new Error(UmpireException.Code, e.ToString());
        }
    }

    protected abstract Task<TResponse> Run(TRequest request, CancellationToken ct);
}
