using VSA.Architecture;

namespace Lib;

public record LibLayer : Layer {
    public override string Folder => "Lib";
}

public sealed class LibDefinition : ArchitectureDefinition {
    protected override void Configure(Options options) {
        options.UseSameLayer = true;
    }

    protected override void Define(Root root) {
        root.Add<LibLayer>();
    }
}
