namespace Umpire.Vsa.Contracts.Requests;

using Umpire.Messages.Contracts;

public interface IRequest : IMessage {
#if NET
    static virtual string? Group => null;
    static virtual string? Name => null;
    static virtual Effect Effect => Effect.Write;
#endif
}

public interface IRequest<TResponse> : IRequest where TResponse : IResponse;
