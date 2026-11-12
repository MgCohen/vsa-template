using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shared;
using Microsoft.JSInterop;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed class ElkLayout(IJSRuntime js) : ILayoutEngine, IAsyncDisposable
{
    public const string Script = "./_content/ArchitectureViewer.Infrastructure/elk-interop.js";

    private IJSObjectReference? elk;

    public async Task<LayoutData> Arrange(LayoutData layout)
    {
        elk ??= await js.InvokeAsync<IJSObjectReference>("import", Script);
        ElkResult result = await elk.InvokeAsync<ElkResult>("layout", ElkGraph.Of(layout));
        return result.ToLayout(layout);
    }

    public async ValueTask DisposeAsync()
    {
        if (elk is not null)
        {
            await elk.DisposeAsync();
        }
    }
}
