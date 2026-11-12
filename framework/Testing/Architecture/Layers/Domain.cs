using VSA.Architecture;

namespace VSA.Testing.Architecture;

public record Domain : Layer {
    public record Shared : Layer;
}
