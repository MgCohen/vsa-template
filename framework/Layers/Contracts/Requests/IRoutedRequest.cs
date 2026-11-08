namespace VSA.Contracts.Requests;

// Each request gives its HTTP method and route on the type itself.
// The HTTP API of the web host reads them to map the route. Only net10.0 has the members, so a request also compiles in Unity.
public interface IRoutedRequest {
#if NET
    static abstract HttpMethod Method { get; }
    static abstract string Route { get; }
#endif
}
