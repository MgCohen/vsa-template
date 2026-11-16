namespace Umpire.Vsa.Definition;

public record Slice : Layer {
    public override string GroupFolder => "Slices";

    public record Shared : Layer {
        public override string Folder => "Slices/Shared";
    }
}
