namespace Steps.Domain.Shared;

[Flags]
public enum Stage {
    Build = 1,
    Check = 2,
    All = Build | Check,
}
