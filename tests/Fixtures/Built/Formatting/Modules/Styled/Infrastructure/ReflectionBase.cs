using VSA.Analyzers.Markers;

namespace Styled.Infrastructure;

[ReflectionBase]
internal abstract class ReflectedScreen
{
}

internal sealed class ReflectedMenu : ReflectedScreen
{
    private readonly int shownCount;

    public ReflectedMenu(int shownCount)
    {
        this.shownCount = shownCount;
    }

    private static void OnShowCalledByReflection()
    {
    }
}

[ReflectionBase]
internal interface IReflectedView
{
}

internal sealed class ReflectedPopup : IReflectedView
{
    private static void OnOpenCalledByReflection()
    {
    }
}
