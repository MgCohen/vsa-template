using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters;

internal sealed record Board(LayoutData Data, LayoutData Layout, string Shape, Viewport View);
