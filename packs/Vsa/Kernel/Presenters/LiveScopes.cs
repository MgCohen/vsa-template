namespace Umpire.Vsa.Presenters;

public sealed class LiveScopes(IServiceProvider rootServices) {
    readonly object gate = new();
    readonly HashSet<PresenterScope> scopes = [];

    internal IServiceProvider RootServices => rootServices;

    internal IReadOnlyList<PresenterScope> Now() {
        lock (gate) {
            return [.. scopes];
        }
    }

    internal void Add(PresenterScope scope) {
        lock (gate) {
            scopes.Add(scope);
        }
    }

    internal void Remove(PresenterScope scope) {
        lock (gate) {
            scopes.Remove(scope);
        }
    }

    internal bool IsLive(PresenterScope scope) {
        lock (gate) {
            return scopes.Contains(scope);
        }
    }
}
