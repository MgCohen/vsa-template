namespace Umpire.Messages.Contracts;

public enum EnvelopeStage {
    Waiting,
    Validate,
    BeforeSteps,
    BeforeHooks,
    Handle,
    AfterHooks,
    AfterSteps,
    Done,
}
