namespace Till.Slices.Put;

using Till.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record PutCommand<T>(int Amount) : ICommand<int> where T : struct, IAmount;
