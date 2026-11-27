namespace Umpire.Architecture;

public sealed record Decision(PartRule? Rule, Permission Default) {
    public Permission Permission => Rule?.Permission ?? Default;
}
