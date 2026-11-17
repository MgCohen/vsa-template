namespace OneLine.App.Host;

using Umpire.Kernel;
using Umpire.Vsa;
using Umpire.Vsa.Composition;

public sealed class OneLineApp : App<Vsa<ConsoleVsaHost>> {
    public const string Name = "a line that is longer than sixty characters";
}
