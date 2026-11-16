namespace SampleApp.Features.SampleModule.Slices.SampleChange;

using Umpire.Vsa.Contracts.Commands;

public sealed record SampleChangeCommand : ICommand<SampleChangeResult>;
