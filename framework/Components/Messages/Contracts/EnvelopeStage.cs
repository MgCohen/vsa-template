namespace VSA.Contracts;

public enum EnvelopeStage {
    Waiting,
    Validate,
    BeforeSteps,
    BeforeEvents,
    Handle,
    AfterEvents,
    AfterSteps,
    Done,
}
