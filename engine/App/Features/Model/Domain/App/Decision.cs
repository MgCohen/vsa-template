namespace Umpire.CheckModel;

public sealed record Decision(UseRule? Rule, UsePair? Pair, Permission Default) {
    public Permission Permission => Rule?.Permission ?? Default;
    public Access? Access => Pair?.Access;
}
