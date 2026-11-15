namespace SampleApp.Plugins.SamplePlugin.Contracts.Commands;

using VSA.Contracts.Commands;

// A base for the commands of each module. A command of a module that derives from it goes to Contracts/Commands.
public abstract record SamplePluginStep<TResult> : ICommand<TResult>;
