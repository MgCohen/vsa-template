namespace VSA.Presenters;

// The state of one operation of a presenter. Views read it and do not change it.
public sealed class OpState {
    public bool Busy { get; internal set; }
    public string? Error { get; internal set; }
}
