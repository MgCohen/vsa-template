namespace Umpire.Vsa.Contracts.Requests;

using Umpire.Messages.Contracts;

public interface IRequest : IMessage {
#if NET
    static abstract HttpMethod Method { get; }
    static abstract string Route { get; }
#endif
}

public interface IRequest<TResponse> : IRequest where TResponse : IResponse;
