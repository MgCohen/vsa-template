namespace Umpire.Vsa.Contracts.Requests;

using Umpire.Messages.Contracts;

public interface IRequestSender {
    Task<Result<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse;
}
