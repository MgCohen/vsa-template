namespace ArchitectureViewer.Presenters.Gestures;

public sealed class KeyboardShortcutsBehavior : Behavior
{
    private readonly Dictionary<string, Func<GraphPresenter, ValueTask>> shortcuts = [];

    public KeyboardShortcutsBehavior(Canvas canvas)
        : base(canvas)
    {
        SetShortcut("Delete", false, false, false, KeyboardShortcutsDefaults.DeleteSelection);
        Canvas.KeyDown += OnKeyDown;
    }

    public void SetShortcut(string key, bool ctrl, bool shift, bool alt, Func<GraphPresenter, ValueTask> action)
    {
        shortcuts[KeyOf(ctrl, shift, alt, key)] = action;
    }

    public bool RemoveShortcut(string key, bool ctrl, bool shift, bool alt)
    {
        return shortcuts.Remove(KeyOf(ctrl, shift, alt, key));
    }

    protected override void Dispose(bool disposing)
    {
        Canvas.KeyDown -= OnKeyDown;
    }

    private static string KeyOf(bool ctrl, bool shift, bool alt, string key)
    {
        return (ctrl ? "Ctrl+" : "") + (shift ? "Shift+" : "") + (alt ? "Alt+" : "") + key;
    }

    private async void OnKeyDown(KeyInput e)
    {
        string key = KeyOf(e.CtrlKey, e.ShiftKey, e.AltKey, e.Key);
        if (shortcuts.TryGetValue(key, out Func<GraphPresenter, ValueTask>? action))
        {
            await action(Canvas.Graph);
        }
    }
}
