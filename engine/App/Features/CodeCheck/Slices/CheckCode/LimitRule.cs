namespace CodeCheck.Slices.CheckCode;

public abstract class LimitRule : KernelRule {
    public const string MaxKey = "max";

    public override IReadOnlyList<RuleOption> Options => [RuleOption.Number(MaxKey, DefaultMax)];

    protected abstract int DefaultMax { get; }

    protected static int? Max(RuleOptions options) => options.Number(MaxKey);
}
