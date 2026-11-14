using ArchitectureViewer.Presenters.Gestures;
using Microsoft.AspNetCore.Components.Web;

namespace App.Views.Graph;

public static class InputMapping
{
    public static PointerInput ToInput(this PointerEventArgs e)
    {
        return new(e.ClientX, e.ClientY, e.Button, e.Buttons, e.CtrlKey, e.ShiftKey);
    }

    public static WheelInput ToInput(this WheelEventArgs e)
    {
        return new(e.ClientX, e.ClientY, e.DeltaY);
    }

    public static KeyInput ToInput(this KeyboardEventArgs e)
    {
        return new(e.Key, e.CtrlKey, e.ShiftKey, e.AltKey);
    }
}
