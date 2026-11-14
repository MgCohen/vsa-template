namespace ArchitectureViewer.Presenters.Gestures;

public static class KeyboardShortcutsDefaults
{
    public static async ValueTask DeleteSelection(GraphPresenter graph)
    {
        if (graph.Editing && graph.Selection.EdgeId is { } edgeId)
        {
            await graph.RemoveLink(edgeId);
        }
    }
}
