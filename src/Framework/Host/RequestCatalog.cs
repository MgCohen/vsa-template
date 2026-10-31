namespace VSA.Host;

// All requests that have a handler. The host maps one HTTP endpoint for each.
sealed class RequestCatalog {
    readonly List<(Type Request, Type Response)> all = [];

    public IReadOnlyList<(Type Request, Type Response)> All => all;

    public void Add(Type request, Type response) {
        if (all.Any(r => r.Request == request)) {
            throw new InvalidOperationException($"{request.Name} has more than one handler.");
        }
        all.Add((request, response));
    }
}
