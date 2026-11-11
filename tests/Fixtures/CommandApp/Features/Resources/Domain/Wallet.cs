using Resources.Domain.Shared;

namespace Resources.Domain;

// The private rules of Resources. Each key starts at 10.
public sealed class Wallet {
    readonly Dictionary<ResourceKey, int> amounts = [];

    public bool TryChange(ResourceKey key, int delta, out int value) {
        var current = amounts.GetValueOrDefault(key, 10);
        if (current + delta < 0) {
            value = current;
            return false;
        }
        value = amounts[key] = current + delta;
        return true;
    }
}
