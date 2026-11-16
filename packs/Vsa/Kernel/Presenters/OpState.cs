namespace Umpire.Vsa.Presenters;

public sealed class OpState {
    public bool Busy { get; internal set; }
    public string? Error { get; internal set; }
}
