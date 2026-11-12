namespace Styled.Infrastructure;

internal sealed class UnmarkedScreen
{
    private readonly int shownCount;
    private readonly int writtenNeverRead;

    public UnmarkedScreen(int shownCount, int writtenNeverRead)
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
