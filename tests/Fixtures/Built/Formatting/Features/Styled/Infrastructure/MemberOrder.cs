namespace Styled.Infrastructure;

// UMP010105: the field count comes after the method Next.
public sealed class MemberOrder {
    public int Next() => ++count;

    int count;
}
