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

internal abstract class ReflectedDialog : ReflectedScreen
{
}

internal sealed class ReflectedConfirm : ReflectedDialog
{
    private int answerSetByReflection;

    public int Answer()
    {
        return answerSetByReflection;
    }

    public void OnClickCalledByReflection(int clickCount)
    {
    }
}

[ReflectionBase]
internal abstract class ReflectedList<TItem>
{
}

internal sealed class ReflectedNames : ReflectedList<string>
{
    private static void OnLoadCalledByReflection()
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

internal interface IReflectedPanel : IReflectedView
{
}

internal sealed class ReflectedSidebar : IReflectedPanel
{
    private int widthNeverUsed;
}

internal static class ReflectedHost
{
    private sealed class NestedScreen : ReflectedScreen
    {
        private static void OnNestedCalledByReflection()
        {
        }
    }
}
