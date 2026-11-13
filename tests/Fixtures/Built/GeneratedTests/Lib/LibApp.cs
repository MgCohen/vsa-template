using VSA.Architecture;

namespace Lib;

public sealed class LibApp : IArchitectureProvider {
    public ArchitectureDefinition Architecture => new LibDefinition();
}
