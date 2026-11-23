namespace Umpire.Vsa.Contracts.Requests;

using Umpire.Messages.Contracts;

public interface IRequest : IMessage;

public interface IRequest<TResponse> : IRequest where TResponse : IResponse;
