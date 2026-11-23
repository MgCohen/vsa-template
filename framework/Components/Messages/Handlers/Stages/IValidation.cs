namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IValidation {
    Error? Validate(Envelope envelope);
}
