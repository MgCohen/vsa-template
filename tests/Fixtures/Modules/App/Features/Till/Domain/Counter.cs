namespace Till.Domain;

using Till.Domain.Shared;

public sealed class Counter<T> : ICounter<T> where T : struct, IAmount;
