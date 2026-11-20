namespace Relay.Slices.Pass;

using Umpire.Vsa.Contracts.Commands;

public record RelayCommand<T>(T Value) : ICommand<string>;
