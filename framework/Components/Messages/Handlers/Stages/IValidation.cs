using VSA.Contracts;

namespace VSA.Handlers;

interface IValidation {
    Error? Validate(Envelope envelope);
}
