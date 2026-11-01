namespace VSA.Contracts.Requests;

// Each request gives its HTTP method and route on the type itself.
// RoutedHandler reads them to register the endpoint, and HttpBus reads them to send the request.
public interface IRoutedRequest {
    static abstract HttpMethod Method { get; }
    static abstract string Route { get; }
}
