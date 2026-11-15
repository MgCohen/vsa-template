using VSA.Architecture;

namespace VSA.Architecture.Vsa;

public record Kernel : External {
    public override string Assembly => "VSA";
}
