namespace Till.Domain;

using Till.Domain.Shared;
using Till.Slices.Put;

public sealed class Entry<T> : IEntry<PutCommand<T>> where T : struct, IAmount {
    Type IEntry<PutCommand<T>>.Entry => GetType();
}
