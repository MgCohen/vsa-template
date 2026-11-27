namespace SampleApp.Plugins.SamplePlugin.Contracts.Commands;

using Umpire.Vsa.Contracts.Commands;

public abstract record SamplePluginStep<TResult> : ICommand<TResult>;
