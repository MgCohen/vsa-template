namespace Umpire.Vsa.Presenters;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public interface IBus {
    Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse;
}
