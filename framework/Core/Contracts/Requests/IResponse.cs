namespace VSA.Contracts.Requests;

// The marker of a response type. IRequest, RoutedHandler and IBus require it on the response,
// so the compiler finds a response without the marker. VSA.Sdk also uses it to find the contract files of a slice.
public interface IResponse;
