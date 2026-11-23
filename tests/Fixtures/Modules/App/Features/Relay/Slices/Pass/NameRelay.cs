namespace Relay.Slices.Pass;

public sealed record NameRelay(string Name) : RelayCommand<string>(Name);
