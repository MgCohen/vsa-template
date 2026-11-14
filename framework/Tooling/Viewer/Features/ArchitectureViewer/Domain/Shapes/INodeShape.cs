using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Shapes;

public interface INodeShape
{
    string Name { get; }

    Node Shape(Node node);
}
