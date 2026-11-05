using VSA.Contracts;

namespace VSA.Contracts.Commands;

// A command goes from one module to another in the same process. It has one handler and one result.
// The result is a type of the same slice. VSA.Sdk puts it in Contracts.Commands, because the command uses it.
// A command has no route: a module sends it with ICommandSender, and HTTP does not see it.
public interface ICommand<TResult> : IMessage;
