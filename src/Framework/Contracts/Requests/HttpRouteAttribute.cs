namespace VSA.Contracts.Requests;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpRouteAttribute(string method, string route) : Attribute {
    public string Method { get; } = method;
    public string Route { get; } = route;
}
