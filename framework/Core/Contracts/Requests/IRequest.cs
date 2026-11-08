using VSA.Contracts;

namespace VSA.Contracts.Requests;

// Each request names its response type and gives its own method and route.
// The base is an interface, because only an interface can make each request give the static members.
// A request is a message (IMessage), as a command is. A request comes from outside, over HTTP.
public interface IRequest<TResponse> : IRoutedRequest, IMessage where TResponse : IResponse;
