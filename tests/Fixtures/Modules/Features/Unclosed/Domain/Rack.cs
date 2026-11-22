namespace Unclosed.Domain;

using Unclosed.Domain.Shared;

public sealed class Rack<T> : IRack<T> where T : struct, IUnused;
