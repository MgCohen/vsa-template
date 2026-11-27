namespace CodeCheck.Slices.CheckCode;

public abstract class VsaRule : CodeRule {
    public sealed override int Group => 2;

    public override string? PackNamespace => "Umpire.Vsa";
}
