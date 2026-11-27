namespace Umpire.Engine.Polyfills;

static class CollectionPolyfills {
    public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> items, IComparer<T>? comparer = null) =>
        items.OrderBy(item => item, comparer);

    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> key) =>
        items.DistinctBy(key, null);

    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> items, Func<T, TKey> key, IEqualityComparer<TKey>? comparer) {
        var seen = new HashSet<TKey>(comparer);
        foreach (var item in items) {
            if (seen.Add(key(item))) {
                yield return item;
            }
        }
    }

    public static IEnumerable<T> TakeLast<T>(this IReadOnlyList<T> items, int count) =>
        items.Skip(Math.Max(0, items.Count - count));

    public static bool TryDequeue<T>(this Queue<T> queue, out T item) {
        if (queue.Count == 0) {
            item = default!;
            return false;
        }
        item = queue.Dequeue();
        return true;
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items, IEqualityComparer<T>? comparer = null) =>
        new(items, comparer);

    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out var value) ? value : default;

    public static TValue GetValueOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue fallback) =>
        dictionary.TryGetValue(key, out var value) ? value : fallback;

    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            return false;
        }
        dictionary.Add(key, value);
        return true;
    }

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value) {
        key = pair.Key;
        value = pair.Value;
    }
}
