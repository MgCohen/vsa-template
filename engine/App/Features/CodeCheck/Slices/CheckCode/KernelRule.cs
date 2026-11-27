namespace CodeCheck.Slices.CheckCode;

public abstract class KernelRule : CodeRule {
    public sealed override int Group => 1;

    public override bool IsModuleRule => false;
}
