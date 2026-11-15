namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

interface IValidation {
    Error? Validate(Envelope envelope);
}
