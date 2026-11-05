using VSA.Contracts.Commands;

namespace SampleModule.Slices.SampleChange;

// Add the input of the command as record parameters. Other modules send the command with ICommandSender.
public sealed record SampleChangeCommand : ICommand<SampleChangeResult>;
