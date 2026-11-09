using VSA.Contracts;

namespace VSA.Handlers;

interface IValidation {
    int Order { get; }

    Error? Validate(Envelope envelope);
}
