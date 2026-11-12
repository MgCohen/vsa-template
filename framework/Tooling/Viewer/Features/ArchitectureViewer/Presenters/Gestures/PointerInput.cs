namespace ArchitectureViewer.Presenters.Gestures;

public sealed record PointerInput(
    double ClientX,
    double ClientY,
    long Button,
    long Buttons,
    bool CtrlKey,
    bool ShiftKey);
