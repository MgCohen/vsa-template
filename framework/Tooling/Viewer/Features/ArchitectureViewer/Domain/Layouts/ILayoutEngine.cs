using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Layouts;

public interface ILayoutEngine
{
    Task<LayoutData> Arrange(LayoutData layout);
}
