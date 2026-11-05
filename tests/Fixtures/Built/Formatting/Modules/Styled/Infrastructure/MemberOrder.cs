namespace Styled.Infrastructure;

// VSA1012: the field count comes after the method Next.
public sealed class MemberOrder {
    public int Next() => ++count;

    int count;
}
