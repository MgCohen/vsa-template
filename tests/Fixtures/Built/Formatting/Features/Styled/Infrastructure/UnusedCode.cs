namespace Styled.Infrastructure;

internal sealed class UnusedScreen
{
    private readonly int shownCount;
    private readonly int writtenNeverRead;

    public UnusedScreen(int shownCount, int writtenNeverRead)
    {
        this.shownCount = shownCount;
        this.writtenNeverRead = writtenNeverRead;
    }

    public int Count(int unusedParameter)
    {
        return shownCount;
    }

    private static void OnShowNeverCalled()
    {
    }
}
