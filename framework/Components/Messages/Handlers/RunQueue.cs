using VSA.Contracts;

namespace VSA.Handlers;

sealed class RunQueue {
    readonly object gate = new();
    readonly LinkedList<Item> items = new();
    bool draining;

    internal int Count {
        get {
            lock (gate) {
                return items.Count;
            }
        }
    }

    internal bool Draining {
        get {
            lock (gate) {
                return draining;
            }
        }
    }

    internal Task Add(Envelope envelope, Func<Envelope, Task> run, bool first) {
        var item = new Item(envelope, run);
        lock (gate) {
            if (first) {
                items.AddFirst(item);
            } else {
                items.AddLast(item);
            }
        }
        return item.Done.Task;
    }

    internal async Task DrainAsync() {
        lock (gate) {
            if (draining) {
                return;
            }
            draining = true;
        }
        try {
            while (Take() is { } item) {
                await item.RunAsync();
            }
        } finally {
            lock (gate) {
                draining = false;
            }
        }
    }

    Item? Take() {
        lock (gate) {
            if (items.First is null) {
                return null;
            }
            var item = items.First.Value;
            items.RemoveFirst();
            return item;
        }
    }

    sealed class Item(Envelope envelope, Func<Envelope, Task> run) {
        internal TaskCompletionSource<bool> Done { get; } = new();

        internal async Task RunAsync() {
            try {
                await run(envelope);
                Done.TrySetResult(true);
            } catch (Exception e) {
                Done.TrySetException(e);
            }
        }
    }
}
