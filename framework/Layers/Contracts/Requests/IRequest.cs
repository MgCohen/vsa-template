namespace VSA.Contracts.Requests;

// Each request names its response type and gives its own method and route.
// The base is an interface, because only an interface can make each request give the static members.
public interface IRequest<TResponse> : IRoutedRequest;
