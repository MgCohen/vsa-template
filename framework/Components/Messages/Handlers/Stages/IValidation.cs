using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

interface IValidation {
    Error? Validate(Envelope envelope);
}
