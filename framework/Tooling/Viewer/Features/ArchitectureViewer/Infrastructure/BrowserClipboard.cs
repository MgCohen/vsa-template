using ArchitectureViewer.Domain;
using Microsoft.JSInterop;

namespace ArchitectureViewer.Infrastructure;

public sealed class BrowserClipboard(IJSRuntime js) : IClipboard
{
    public async Task<bool> Copy(string text)
    {
        try
        {
            await js.InvokeVoidAsync("navigator.clipboard.writeText", text);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }
}
