namespace VsaApp.Plugins.Tools.Infrastructure;

using VsaApp.Plugins.Clock.Infrastructure;

public sealed class ToolClock(ClockSource source) {
    public DateTime Now => source.Now;
}
