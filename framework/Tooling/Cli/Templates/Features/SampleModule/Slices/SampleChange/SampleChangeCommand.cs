namespace SampleApp.Features.SampleModule.Slices.SampleChange;

using VSA.Contracts.Commands;

// Add the input of the command as record parameters. Other modules send the command with ICommandSender.
public sealed record SampleChangeCommand : ICommand<SampleChangeResult>;
